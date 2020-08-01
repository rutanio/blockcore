﻿using System.Collections.Generic;
using System.Threading.Tasks;
using Blockcore.Controllers;
using Blockcore.Features.Storage.Models;
using Blockcore.Features.Storage.Persistence;
using MessagePack;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NBitcoin;
using Newtonsoft.Json;

namespace Blockcore.Features.Storage.Controllers
{
    /// <summary>
    /// All write operations to the identity controller must be signed and will be validated. 
    /// Identities can only be edited by the owner of the private key, which must sign the data before submitting to the API.
    /// </summary>
    [Authorize]
    [Route("api/identity")]
    public class IdentityController : FeatureController
    {
        private readonly DataStore dataStore;

        private readonly StorageSchemas schemas;

        private readonly StorageFeature storageFeature;

        public IdentityController(IDataStore dataStore, StorageSchemas schemas, StorageFeature storageFeature)
        {
            this.dataStore = (DataStore)dataStore;
            this.schemas = schemas;
            this.storageFeature = storageFeature;
        }

        /// <summary>
        /// Returns all registered identities. This API will be removed and is only available for testing.
        /// </summary>
        /// <returns></returns>
        [HttpGet("")]
        public async Task<IActionResult> GetIdentities()
        {
            IEnumerable<IdentityDocument> identities = this.dataStore.GetIdentities();
            return Ok(identities);
        }

        /// <summary>
        /// Retrieve the profile of an identity.
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        [HttpGet("{address}")]
        public async Task<IActionResult> GetIdentity([FromRoute] string address)
        {
            if (string.IsNullOrWhiteSpace(address))
            {
                return BadRequest();
            }

            IdentityDocument document = this.dataStore.GetIdentity(address);

            if (document == null)
            {
                return NotFound();
            }

            return Ok(document);
        }

        /// <summary>
        /// Persist the profile of an identity.
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        [HttpPut("{address}")]
        public async Task<IActionResult> PutIdentity([FromRoute] string address, [FromBody] IdentityDocument document)
        {
            if (string.IsNullOrWhiteSpace(address))
            {
                return BadRequest();
            }

            // Make sure that only identity documents is submitted to this API.
            if (document.GetContainer() != "identity")
            {
                return Problem("Container must be identity.");
            }

            // Make sure the route address and document owner is the same.
            if (address != document.GetIdentifier())
            {
                return BadRequest();
            }

            if (!this.schemas.SupportedIdentityVersion(document.Version))
            {
                return Problem(title: "Incompatible version", detail: $"Unsupported document version: {document.Version}. Supported range: {this.schemas.IdentityMinVersion}-{this.schemas.IdentityMaxVersion}.", statusCode: 400);
            }

            byte[] entityBytes = MessagePackSerializer.Serialize(document.Content);

            var bitcoinAddress = (BitcoinPubKeyAddress)BitcoinPubKeyAddress.Create(address, ProfileNetwork.Instance);

            var valid = bitcoinAddress.VerifyMessage(entityBytes, document.Signature);

            if (!valid)
            {
                // Invalid signature.
                return BadRequest();
            }

            this.dataStore.SetIdentity(document);

            string json = JsonConvert.SerializeObject(document, JsonSettings.Storage);

            // Announce the recently observed identity to connected nodes.
            await this.storageFeature.AnnounceDocument("identity", json);

            return Ok(valid.ToString());
        }

        /// <summary>
        /// Remove the profile of an identity.
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        [HttpDelete("{address}")]
        public async Task<IActionResult> DeleteIdentity([FromRoute] string address)
        {
            if (string.IsNullOrWhiteSpace(address))
            {
                return BadRequest();
            }

            IdentityDocument document = this.dataStore.GetIdentity(address);

            if (document == null)
            {
                return NotFound();
            }

            // Perform validation of signature and allow delete.

            return Ok(address);
        }
    }
}
