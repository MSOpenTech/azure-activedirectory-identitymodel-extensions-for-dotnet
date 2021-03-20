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
using System.Runtime.Serialization;

namespace Microsoft.IdentityModel.Tokens
{
    /// <summary>
    /// This exception is thrown when a security token contained a key identifier but the key was not found by the runtime.
    /// In addition, when validation errors exist over the security token.
    /// </summary>
    [Serializable]
    public class SecurityTokenSignatureKeyNotFoundWithValidationErrorsException
        : SecurityTokenException
    {
        [NonSerialized]
        const string _Prefix = "Microsoft.IdentityModel." + nameof(SecurityTokenSignatureKeyNotFoundWithValidationErrorsException) + ".";

        [NonSerialized]
        const string _ValidIssuerKey = _Prefix + nameof(ValidIssuer);

        [NonSerialized]
        const string _ValidLifetimeKey = _Prefix + nameof(ValidLifetime);

        /// <summary>
        /// Indicates if the issuer was valid or not.
        /// </summary>
        public bool ValidIssuer { get; set; }

        /// <summary>
        /// Indiciates if the life was valid or not.
        /// </summary>
        public bool ValidLifetime { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SecurityTokenSignatureKeyNotFoundException"/> class.
        /// </summary>
        public SecurityTokenSignatureKeyNotFoundWithValidationErrorsException()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SecurityTokenSignatureKeyNotFoundException"/> class.
        /// </summary>
        /// <param name="validIssuer"><c>true</c> if the issuer is valid; otherwise <c>false</c></param>
        /// <param name="validLifetime"><c>true</c> if the lifetime is valid; otherwise <c>false</c></param>
        /// <param name="message">Addtional information to be included in the exception and displayed to user.</param>
        public SecurityTokenSignatureKeyNotFoundWithValidationErrorsException(bool validIssuer, bool validLifetime, string message)
            : base(message)
        {
            ValidIssuer = validIssuer;
            ValidLifetime = validLifetime;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SecurityTokenSignatureKeyNotFoundException"/> class.
        /// </summary>
        /// <param name="message">Addtional information to be included in the exception and displayed to user.</param>
        public SecurityTokenSignatureKeyNotFoundWithValidationErrorsException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SecurityTokenSignatureKeyNotFoundException"/> class.
        /// </summary>
        /// <param name="message">Addtional information to be included in the exception and displayed to user.</param>
        /// <param name="innerException">A <see cref="Exception"/> that represents the root cause of the exception.</param>
        public SecurityTokenSignatureKeyNotFoundWithValidationErrorsException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SecurityTokenSignatureKeyNotFoundException"/> class.
        /// </summary>
        /// <param name="info">the <see cref="SerializationInfo"/> that holds the serialized object data.</param>
        /// <param name="context">The contextual information about the source or destination.</param>
        protected SecurityTokenSignatureKeyNotFoundWithValidationErrorsException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            SerializationInfoEnumerator enumerator = info.GetEnumerator();
            while (enumerator.MoveNext())
            {
                switch (enumerator.Name)
                {
                    case _ValidIssuerKey:
                        ValidIssuer = info.GetBoolean(_ValidIssuerKey);
                        break;

                    case _ValidLifetimeKey:
                        ValidLifetime = info.GetBoolean(_ValidLifetimeKey);
                        break;

                    default:
                        // Ignore other fields.
                        break;
                }
            }
        }

        /// <inheritdoc/>
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);

            info.AddValue(_ValidIssuerKey, ValidIssuer);
            info.AddValue(_ValidLifetimeKey, ValidLifetime);
        }
    }
}