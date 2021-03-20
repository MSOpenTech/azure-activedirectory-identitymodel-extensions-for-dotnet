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
using System.Text;
using Microsoft.IdentityModel.Logging;
using TokenLogMessages = Microsoft.IdentityModel.Tokens.LogMessages;

namespace Microsoft.IdentityModel.Tokens
{
    /// <summary>
    /// Validators meant to be kept internal
    /// </summary>
    internal static class InternalValidators
    {
        /// <summary>
        /// Called after signature validity has already been determined
        /// </summary>
        internal static void ValidateLifetimeAndIssuerAfterSignatureNotValidatedJwt(SecurityToken securityToken, DateTime? notBefore, DateTime? expires, string kid, TokenValidationParameters validationParameters, StringBuilder exceptionStrings)
        {
            bool validIssuer = false;
            bool validLifetime = false;

            try
            {
                Validators.ValidateLifetime(notBefore, expires, securityToken, validationParameters);
                validLifetime = true;
            }
            catch (Exception)
            {
                // validLifetime will remain false
            }

            try
            {
                Validators.ValidateIssuer(securityToken.Issuer, securityToken, validationParameters);
                validIssuer = true;
            }
            catch (Exception)
            {
                // validIssuer will remain false
            }

            if (validLifetime && validIssuer)
                throw LogHelper.LogExceptionMessage(new SecurityTokenSignatureKeyNotFoundException(LogHelper.FormatInvariant(TokenLogMessages.IDX10501, kid, exceptionStrings, securityToken)));
            else
                throw LogHelper.LogExceptionMessage(new SecurityTokenSignatureKeyNotFoundWithValidationErrorsException(
                    validIssuer,
                    validLifetime,
                    LogHelper.FormatInvariant(TokenLogMessages.IDX10516, kid, exceptionStrings, securityToken, validLifetime, validIssuer)));
        }

        /// <summary>
        /// Called after signature validity has already been determined
        /// </summary>
        internal static void ValidateLifetimeAndIssuerAfterSignatureNotValidatedSaml(SecurityToken securityToken, DateTime? notBefore, DateTime? expires, string keyInfo, TokenValidationParameters validationParameters, StringBuilder exceptionStrings)
        {
            bool validIssuer = false;
            bool validLifetime = false;

            try
            {
                Validators.ValidateLifetime(notBefore, expires, securityToken, validationParameters);
                validLifetime = true;
            }
            catch (Exception)
            {
                // validLifetime will remain false
            }

            try
            {
                Validators.ValidateIssuer(securityToken.Issuer, securityToken, validationParameters);
                validIssuer = true;
            }
            catch (Exception)
            {
                // validIssuer will remain false
            }

            if (validLifetime && validIssuer)
                throw LogHelper.LogExceptionMessage(new SecurityTokenSignatureKeyNotFoundException(LogHelper.FormatInvariant(TokenLogMessages.IDX10513, keyInfo, exceptionStrings, securityToken)));
            else
                throw LogHelper.LogExceptionMessage(new SecurityTokenSignatureKeyNotFoundWithValidationErrorsException(
                    validIssuer,
                    validLifetime,
                    LogHelper.FormatInvariant(TokenLogMessages.IDX10515, keyInfo, exceptionStrings, securityToken, validLifetime, validIssuer)));
        }

    }
}