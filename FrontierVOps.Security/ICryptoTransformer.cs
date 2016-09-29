using System;
using System.Security.Cryptography;

namespace FrontierVOps.Security
{
    internal interface ICryptoTransformer
    {
        /// <summary>
        /// Initialization Vector
        /// </summary>
        byte[] IV { get; set; }

        /// <summary>
        /// Symmetric Encryption Key
        /// </summary>
        byte[] EncryptionKey { get; set; }

        /// <summary>
        /// Creates symmetric encryption/decryption object, and creates IV based on algorithm
        /// </summary>
        /// <param name="bytesKey">Encryption Key Bytes</param>
        /// <param name="algorithmID">Encryption Algorithm</param>
        /// <returns>Encryption/Decryption Object</returns>
        ICryptoTransform CreateCypher(byte[] bytesKey, Algorithm algorithmID);
    }
}
