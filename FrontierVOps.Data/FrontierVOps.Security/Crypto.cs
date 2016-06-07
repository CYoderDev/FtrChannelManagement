﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Security;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using FrontierVOps.Common;

namespace FrontierVOps.Security
{
    public class Crypto
    {
        #region Encryption
        /// <summary>
        /// Encrypts a secure string.
        /// </summary>
        /// <param name="Input">Value to encrypt</param>
        /// <param name="Key">Encryption key</param>
        /// <param name="EncryptionAlgorithm">Type of encryption algorithm</param>
        /// <returns>Encrypted data as a secure string</returns>
        public static SecureString EncryptSecure(SecureString Input, SecureString Key, Algorithm EncryptionAlgorithm)
        {
            return Toolset.CreateSecureString(Encrypt(Input, Toolset.ConvertToInsecureString(Key), EncryptionAlgorithm));
        }

        /// <summary>
        /// Encrypts a string value
        /// </summary>
        /// <param name="Input">Value to encrypt</param>
        /// <param name="Key">Encryption key</param>
        /// <param name="EncryptionAlgorithm">Type of encryption algorithm</param>
        /// <returns>Encrypted data</returns>
        public static string Encrypt(SecureString Input, string Key, Algorithm EncryptionAlgorithm)
        {
            return Encrypt(Toolset.ConvertToInsecureString(Input), Key, EncryptionAlgorithm);
        }

        /// <summary>
        /// Encrypts a string value
        /// </summary>
        /// <param name="Input">Value to encrypt</param>
        /// <param name="Key">Encryption key</param>
        /// <param name="EncryptionAlgorithm">Type of encryption algorithm</param>
        /// <returns>Encrypted data</returns>
        public static string Encrypt(string Input, string Key, Algorithm EncryptionAlgorithm)
        {
            if (string.IsNullOrEmpty(Key))
                throw new ArgumentNullException("Key cannot be null or empty");

            ICryptoTransformer cryptoTransformer = new EncryptTransform();

            return Convert.ToBase64String(
                Transform(
                    Encoding.ASCII.GetBytes(Input),
                    Encoding.ASCII.GetBytes(Key),
                    EncryptionAlgorithm,
                    cryptoTransformer));
        }
        #endregion //Encryption

        #region Decryption
            
        #endregion //Decryption

        #region Transform
        /// <summary>
        /// Encrypts or decrypts data bytes
        /// </summary>
        /// <param name="dataBytes">Data to encrypt/decrypt</param>
        /// <param name="keyBytes">Key used for encryption/decryption</param>
        /// <param name="algorithmId">Encryption algorithm type</param>
        /// <param name="cryptoTransformer">Cryptographic transformer</param>
        /// <returns>Memory stream of encrypted data</returns>
        private static byte[] Transform(byte[] dataBytes, byte[] keyBytes, Algorithm algorithmId, ICryptoTransformer cryptoTransformer)
        {
            //Get whether or not this is an encryption or decryption transform.
            bool isDecrypt = cryptoTransformer.GetType() != typeof(EncryptTransform);

            //If decrypting, separate the IV from the original data that
            //is stored into the data during encryption.
            if (isDecrypt)
            {
                int ivLength = 0;
                switch (algorithmId)
                {
                    case Algorithm.Rijndael:
                    case Algorithm.TripleDes:
                    case Algorithm.Des:
                    case Algorithm.Rc2:
                        ivLength = 16;
                        break;
                }
                byte[] origData = new byte[dataBytes.Length - ivLength];
                (cryptoTransformer as DecryptTransform).SetIV(new byte[ivLength]);
                Buffer.BlockCopy(dataBytes, 0, cryptoTransformer.IV, 0, 16);
                Buffer.BlockCopy(dataBytes, ivLength, origData, 0, dataBytes.Length - ivLength);
                dataBytes = origData;
            }

            using (MemoryStream memStream = new MemoryStream())
            {
                //If encrypting, add the IV bytes to the data stream first
                if (!isDecrypt)
                    memStream.Write(cryptoTransformer.IV, 0, cryptoTransformer.IV.Length);

                using (CryptoStream cryptoStream = new CryptoStream(memStream, cryptoTransformer.CreateCrypter(keyBytes, algorithmId), CryptoStreamMode.Write))
                {
                    try
                    {
                        cryptoStream.Write(dataBytes, 0, dataBytes.Length);
                    }
                    finally
                    {
                        cryptoStream.FlushFinalBlock();
                        cryptoStream.Close();
                    }
                    return memStream.ToArray();
                }
            }
        }
        #endregion //Transform
    }
}
