using System;
using System.IO;
using System.Security.Cryptography;

namespace FrontierVOps.Security
{
    internal class EncryptTransform : ICryptoTransformer
    {
        /// <summary>
        /// Get the encryption initialization vector.
        /// </summary>
        internal byte[] IV { get { return _iv; } private set { _iv = value; } }
        private byte[] _iv;

        /// <summary>
        /// Get or set the encryption key used to encrypt the data.
        /// </summary>
        internal byte[] EncryptionKey { get { return _encryptionKey; } set { _encryptionKey = value; } }
        private byte[] _encryptionKey;

        internal ICryptoTransform CreateCypher(byte[] bytesKey, Algorithm alorithmID)
        {
            switch (alorithmID)
            {
                case Algorithm.Rijndael:
                    using (Rijndael rij = new RijndaelManaged(){ Padding = PaddingMode.Zeros, Key = this._encryptionKey, Mode = CipherMode.CBC })
                    {
                        this._iv = rij.IV;
                        return rij.CreateEncryptor();
                    }

                case Algorithm.TripleDes:
                    using (TripleDES des3 = new TripleDESCryptoServiceProvider() { Padding = PaddingMode.Zeros, Key = this._encryptionKey, Mode = CipherMode.CBC })
                    {
                        this._iv = des3.IV;
                        return des3.CreateEncryptor();
                    }

                case Algorithm.Des:
                    using (DES des = new DESCryptoServiceProvider() { Padding = PaddingMode.Zeros, Key = this._encryptionKey, Mode = CipherMode.CBC })
                    {
                        this._iv = des.IV;
                        return des.CreateEncryptor();
                    }

                case Algorithm.Rc2:
                    using (RC2 rc2 = new RC2CryptoServiceProvider() { Padding = PaddingMode.Zeros, Key = this._encryptionKey, Mode = CipherMode.CBC })
                    {
                        this._iv = rc2.IV;
                        return rc2.CreateEncryptor();
                    }

                default:
                    throw new CryptographicException(string.Format("Algorithm {0} is not supported.", alorithmID));
            }
        }
    }
}
