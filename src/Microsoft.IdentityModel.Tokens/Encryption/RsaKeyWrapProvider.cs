﻿//------------------------------------------------------------------------------
//
// Copyright (c) Microsoft Corporation.
// All rights reserved.
//
// This code is licensed under the MIT License.
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files(the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and / or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions :
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//
//------------------------------------------------------------------------------

using System;
using System.Security.Cryptography;
using Microsoft.IdentityModel.Logging;

namespace Microsoft.IdentityModel.Tokens
{
    /// <summary>
    /// Provides RSA Wrap key and Unwrap key services.
    /// </summary>
    public class RsaKeyWrapProvider : KeyWrapProvider
    {
#if (NET45 || NET451)     
        private RSACryptoServiceProviderProxy _rsaCryptoServiceProviderProxy;
#endif
        private RSA _rsa;
        private bool _dispose;
        private bool _disposed = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="RsaKeyWrapProvider"/> class used for wrap key and unwrap key.
        /// <param name="key">The <see cref="SecurityKey"/> that will be used for crypto operations.</param>
        /// <param name="algorithm">The KeyWrap algorithm to apply.</param>
        /// <param name="willUnwrap">Whether this <see cref="RsaKeyWrapProvider"/> is required to create decrypts then set this to true.</param>
        /// <exception cref="ArgumentNullException">'key' is null.</exception>
        /// <exception cref="ArgumentNullException">'algorithm' is null.</exception>
        /// <exception cref="ArgumentException">The keysize doesn't match the algorithm.</exception>
        /// <exception cref="ArgumentException">If <see cref="SecurityKey"/> and algorithm pair are not supported.</exception>
        /// <exception cref="InvalidOperationException">Failed to create RSA algorithm with provided key and algorithm.</exception>
        /// </summary>
        public RsaKeyWrapProvider(SecurityKey key, string algorithm, bool willUnwrap)
        {
            if (key == null)
                throw LogHelper.LogArgumentNullException(nameof(key));

            if (string.IsNullOrEmpty(algorithm))
                throw LogHelper.LogArgumentNullException(nameof(algorithm));

            if (!IsSupportedAlgorithm(key, algorithm))
                throw LogHelper.LogExceptionMessage(new NotSupportedException(LogHelper.FormatInvariant(LogMessages.IDX10661, algorithm, key)));

            Algorithm = algorithm;
            Key = key;

            var rsaAlgorithm = RsaAlgorithm.ResolveRsaAlgorithm(key, algorithm, willUnwrap);

#if (NET45 || NET451)
            if (rsaAlgorithm != null && rsaAlgorithm.rsaCryptoServiceProviderProxy != null)
            {
                _rsaCryptoServiceProviderProxy = rsaAlgorithm.rsaCryptoServiceProviderProxy;
                _dispose = rsaAlgorithm.dispose;
                return;
            }

            if (rsaAlgorithm != null && rsaAlgorithm.rsa != null && rsaAlgorithm.rsa as RSACryptoServiceProvider != null)
            {
                _rsaCryptoServiceProviderProxy = new RSACryptoServiceProviderProxy(rsaAlgorithm.rsa as RSACryptoServiceProvider);
                _dispose = rsaAlgorithm.dispose;
                return;
            }
#endif
            if (rsaAlgorithm != null && rsaAlgorithm.rsa != null)
            {
                _rsa = rsaAlgorithm.rsa;
                _dispose = rsaAlgorithm.dispose;
                return;
            }


            throw LogHelper.LogExceptionMessage(new NotSupportedException(LogHelper.FormatInvariant(LogMessages.IDX10661, algorithm, key)));
        }

        /// <summary>
        /// Gets the KeyWrap algorithm that is being used.
        /// </summary>
        public override string Algorithm { get; }

        /// <summary>
        /// Gets or sets a user context for a <see cref="KeyWrapProvider"/>.
        /// </summary>
        /// <remarks>This is null by default. This can be used by runtimes or for extensibility scenarios.</remarks>
        public override string Context { get; set; }

        /// <summary>
        /// Gets the <see cref="SecurityKey"/> that is being used.
        /// </summary>
        public override SecurityKey Key { get; }

        /// <summary>
        /// Disposes of internal components.
        /// </summary>
        /// <param name="disposing">true, if called from Dispose(), false, if invoked inside a finalizer.</param>
        protected override void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    if (_rsa != null && _dispose)
                        _rsa.Dispose();

#if (NET45 || NET451)
                    if (_rsaCryptoServiceProviderProxy != null)
                        _rsaCryptoServiceProviderProxy.Dispose();
#endif
                    _disposed = true;
                }
            }
        }

        /// <summary>
        /// Checks if an algorithm is supported.
        /// </summary>
        /// <param name="key">The <see cref="SecurityKey"/> that will be used for crypto operations.</param>
        /// <param name="algorithm">The KeyWrap algorithm to apply.</param>
        /// <returns>true if the algorithm is supported; otherwise, false.</returns>
        protected virtual bool IsSupportedAlgorithm(SecurityKey key, string algorithm)
        {
            if (key == null)
                return false;

            if (string.IsNullOrEmpty(algorithm))
                return false;

            if (key.KeySize < 2048)
            {
                return false;
            }

            if (algorithm.Equals(SecurityAlgorithms.RsaPKCS1, StringComparison.Ordinal)
             || algorithm.Equals(SecurityAlgorithms.RsaOAEP, StringComparison.Ordinal)
             || algorithm.Equals(SecurityAlgorithms.RsaOaepKeyWrap, StringComparison.Ordinal))
            {
                if (key as RsaSecurityKey != null)
                    return true;

                var x509Key = key as X509SecurityKey;
                if (x509Key != null)
                {
                    if (x509Key.PublicKey != null)
                        return true;

                    return false;
                }

                var jsonWebKey = key as JsonWebKey;
                if (jsonWebKey != null && jsonWebKey.Kty == JsonWebAlgorithmsKeyTypes.RSA)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Unwrap a key using RSA decryption.
        /// </summary>
        /// <param name="keyBytes">the bytes to unwrap.</param>
        /// <returns>Unwrapped key</returns>
        /// <exception cref="ArgumentNullException">'keyBytes' is null or length == 0.</exception>
        /// <exception cref="ObjectDisposedException">If <see cref="RsaKeyWrapProvider.Dispose(bool)"/> has been called.</exception>
        /// <exception cref="SecurityTokenKeyWrapException">Failed to unwrap the wrappedKey.</exception>
        /// <exception cref="InvalidOperationException">If the internal RSA algorithm is null.</exception>
        public override byte[] UnwrapKey(byte[] keyBytes)
        {
            if (keyBytes == null || keyBytes.Length == 0)
                throw LogHelper.LogArgumentNullException("wrappedKey");

            if (_disposed)
                throw LogHelper.LogExceptionMessage(new ObjectDisposedException(GetType().ToString()));

            bool fOAEP = Algorithm.Equals(SecurityAlgorithms.RsaOAEP, StringComparison.Ordinal)
                      || Algorithm.Equals(SecurityAlgorithms.RsaOaepKeyWrap, StringComparison.Ordinal);
            try
            {
                if (_rsa != null)
                    return RunTimeMethodResolver.Decrypt(_rsa, keyBytes, fOAEP);
#if (NET45 || NET451)
                else if (_rsaCryptoServiceProviderProxy != null)
                    return _rsaCryptoServiceProviderProxy.Decrypt(keyBytes, fOAEP);
#endif
            }
            catch (Exception ex)
            {
                throw LogHelper.LogExceptionMessage(new SecurityTokenKeyWrapException(LogHelper.FormatInvariant(LogMessages.IDX10659, ex)));
            }

            throw LogHelper.LogExceptionMessage(new InvalidOperationException(LogHelper.FormatInvariant(LogMessages.IDX10644, Algorithm)));
        }

        /// <summary>
        /// Wrap a key using RSA encryption.
        /// </summary>
        /// <param name="keyBytes">the key to be wrapped</param>
        /// <returns>A wrapped key</returns>
        /// <exception cref="ArgumentNullException">'keyBytes' is null or has length == 0.</exception>
        /// <exception cref="ObjectDisposedException">If <see cref="RsaKeyWrapProvider.Dispose(bool)"/> has been called.</exception>
        /// <exception cref="SecurityTokenKeyWrapException">Failed to wrap the 'keyBytes'.</exception>
        /// <exception cref="InvalidOperationException">If the internal RSA algorithm is null.</exception>
        public override byte[] WrapKey(byte[] keyBytes)
        {
            if (keyBytes == null || keyBytes.Length == 0)
                throw LogHelper.LogArgumentNullException(nameof(keyBytes));

            if (_disposed)
                throw LogHelper.LogExceptionMessage(new ObjectDisposedException(GetType().ToString()));

            bool fOAEP = Algorithm.Equals(SecurityAlgorithms.RsaOAEP, StringComparison.Ordinal)
                      || Algorithm.Equals(SecurityAlgorithms.RsaOaepKeyWrap, StringComparison.Ordinal);
            try
            {
                if (_rsa != null)
                    return RunTimeMethodResolver.Encrypt(_rsa, keyBytes, fOAEP);
#if (NET45 || NET451)
                else if (_rsaCryptoServiceProviderProxy != null)
                    return _rsaCryptoServiceProviderProxy.Encrypt(keyBytes, fOAEP);
#endif
            }
            catch (Exception ex)
            {
                throw LogHelper.LogExceptionMessage(new SecurityTokenKeyWrapException(LogHelper.FormatInvariant(LogMessages.IDX10658, ex)));
            }

            throw LogHelper.LogExceptionMessage(new InvalidOperationException(LogHelper.FormatInvariant(LogMessages.IDX10644, Algorithm)));
        }
    }
}
