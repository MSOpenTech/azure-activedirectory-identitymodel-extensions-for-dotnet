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
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using Microsoft.IdentityModel.Protocols.WsFed;
using Microsoft.IdentityModel.Protocols.WsSecurity;
using Microsoft.IdentityModel.TestUtils;
using Microsoft.IdentityModel.Xml;
using Xunit;

#pragma warning disable CS3016 // Arrays as attribute arguments is not CLS-compliant

namespace Microsoft.IdentityModel.Protocols.WsTrust.Tests
{
    public class WsTrustSerializerTests
    {
        [Fact]
        public void Constructors()
        {
            TestUtilities.WriteHeader($"{this}.Constructors");
            CompareContext context = new CompareContext("Constructors");
            WsTrustSerializer wsTrustSerializer = new WsTrustSerializer();

            if (wsTrustSerializer.SecurityTokenHandlers.Count != 2)
                context.AddDiff("wsTrustSerializer.SecurityTokenHandlers.Count != 2");

            TestUtilities.AssertFailIfErrors(context);
        }

        [Theory, MemberData(nameof(ReadBinarySecrectTheoryData))]
        public void ReadBinarySecrect(WsTrustSerializerTheoryData theoryData)
        {
            var context = TestUtilities.WriteHeader($"{this}.ReadBinarySecrect", theoryData);
            try
            {
                var binarySecret = theoryData.WsTrustSerializer.ReadBinarySecrect(theoryData.Reader, theoryData.WsSerializationContext);
                IdentityComparer.AreEqual(binarySecret, theoryData.BinarySecret, context);
            }
            catch (Exception ex)
            {
                theoryData.ExpectedException.ProcessException(ex, context);
            }

            TestUtilities.AssertFailIfErrors(context);
        }

        public static TheoryData<WsTrustSerializerTheoryData> ReadBinarySecrectTheoryData
        {
            get
            {
                return new TheoryData<WsTrustSerializerTheoryData>
                {
                    new WsTrustSerializerTheoryData(ReferenceXml.RandomElementReader)
                    {
                        ExpectedException = ExpectedException.ArgumentNullException("serializationContext"),
                        First = true,
                        TestId = "SerializationContextNull"
                    },
                    new WsTrustSerializerTheoryData(WsTrustVersion.Trust13)
                    {
                        ExpectedException = ExpectedException.ArgumentNullException("reader"),
                        TestId = "ReaderNull",
                        WsSerializationContext = new WsSerializationContext(WsTrustVersion.Trust13)
                    },
                    new WsTrustSerializerTheoryData(WsTrustVersion.TrustFeb2005)
                    {
                        BinarySecret = new BinarySecret(Convert.FromBase64String(KeyingMaterial.SelfSigned2048_SHA256), WsTrustConstants.TrustFeb2005.WsTrustBinarySecretTypes.AsymmetricKey),
                        Reader = ReferenceXml.GetBinarySecretReader(WsTrustConstants.TrustFeb2005, WsTrustConstants.TrustFeb2005.WsTrustBinarySecretTypes.AsymmetricKey, KeyingMaterial.SelfSigned2048_SHA256),
                        TestId = "TrustFeb2005"
                    },
                    new WsTrustSerializerTheoryData(WsTrustVersion.Trust13)
                    {
                        BinarySecret = new BinarySecret(Convert.FromBase64String(KeyingMaterial.SelfSigned2048_SHA256), WsTrustConstants.Trust13.WsTrustBinarySecretTypes.AsymmetricKey),
                        Reader = ReferenceXml.GetBinarySecretReader(WsTrustConstants.Trust13, WsTrustConstants.Trust13.WsTrustBinarySecretTypes.AsymmetricKey, KeyingMaterial.SelfSigned2048_SHA256),
                        TestId = "Trust13"
                    },
                    new WsTrustSerializerTheoryData(WsTrustVersion.Trust14)
                    {
                        BinarySecret = new BinarySecret(Convert.FromBase64String(KeyingMaterial.SelfSigned2048_SHA256), WsTrustConstants.Trust14.WsTrustBinarySecretTypes.AsymmetricKey),
                        Reader = ReferenceXml.GetBinarySecretReader(WsTrustConstants.Trust14, WsTrustConstants.Trust14.WsTrustBinarySecretTypes.AsymmetricKey, KeyingMaterial.SelfSigned2048_SHA256),
                        TestId = "Trust14"
                    },
                    new WsTrustSerializerTheoryData(WsTrustVersion.Trust13)
                    {
                        ExpectedException = new ExpectedException(typeof(XmlReadException), "IDX30017:", typeof(System.Xml.XmlException)),
                        BinarySecret = new BinarySecret(Convert.FromBase64String(KeyingMaterial.SelfSigned2048_SHA256), WsTrustConstants.Trust13.WsTrustBinarySecretTypes.AsymmetricKey),
                        Reader = ReferenceXml.GetBinarySecretReader(WsTrustConstants.Trust13, WsTrustConstants.Trust13.WsTrustBinarySecretTypes.AsymmetricKey, "xxx"),
                        TestId = "EncodingError"
                    },
                    new WsTrustSerializerTheoryData(WsTrustVersion.Trust14)
                    {
                        ExpectedException = new ExpectedException(typeof(XmlReadException), "IDX30011:"),
                        BinarySecret = new BinarySecret(Convert.FromBase64String(KeyingMaterial.SelfSigned2048_SHA256), WsTrustConstants.Trust13.WsTrustBinarySecretTypes.AsymmetricKey),
                        Reader = ReferenceXml.GetBinarySecretReader(WsTrustConstants.Trust13, WsTrustConstants.Trust13.WsTrustBinarySecretTypes.AsymmetricKey, KeyingMaterial.SelfSigned2048_SHA256),
                        TestId = "Trust13_14"
                    },
                    new WsTrustSerializerTheoryData(WsTrustVersion.Trust13)
                    {
                        ExpectedException = ExpectedException.XmlReadException("IDX30011:"),
                        Reader = ReferenceXml.RandomElementReader,
                        TestId = "ReaderNotOnCorrectElement"
                    }
                };
            }
        }

        [Theory, MemberData(nameof(ReadClaimsTheoryData))]
        public void ReadClaims(WsTrustSerializerTheoryData theoryData)
        {
            var context = TestUtilities.WriteHeader($"{this}.ReadClaims", theoryData);

            try
            {
                var claims = theoryData.WsTrustSerializer.ReadClaims(theoryData.Reader, theoryData.WsSerializationContext);
                theoryData.ExpectedException.ProcessNoException(context);
                IdentityComparer.AreEqual(claims, theoryData.Claims, context);
            }
            catch (Exception ex)
            {
                theoryData.ExpectedException.ProcessException(ex, context);
            }

            TestUtilities.AssertFailIfErrors(context);
        }

        public static TheoryData<WsTrustSerializerTheoryData> ReadClaimsTheoryData
        {
            get
            {
                var theoryData = new TheoryData<WsTrustSerializerTheoryData>
                {
                    new WsTrustSerializerTheoryData(ReferenceXml.RandomElementReader)
                    {
                        ExpectedException = ExpectedException.ArgumentNullException("serializationContext"),
                        First = true,
                        TestId = "SerializationContextNull",
                        WsSerializationContext = null
                    },
                    new WsTrustSerializerTheoryData(WsTrustVersion.Trust13)
                    {
                        ExpectedException = ExpectedException.ArgumentNullException("reader"),
                        Reader = null,
                        TestId = "ReaderNull",
                    },
                    new WsTrustSerializerTheoryData(WsTrustVersion.Trust13)
                    {
                        ExpectedException = ExpectedException.XmlReadException(),
                        Reader = ReferenceXml.RandomElementReader,
                        TestId = "ReaderNotOnCorrectElement",
                    }
                };

                return theoryData;
            }
        }

        [Theory, MemberData(nameof(ReadEntropyTheoryData))]
        public void ReadEntropy(WsTrustSerializerTheoryData theoryData)
        {
            var context = TestUtilities.WriteHeader($"{this}.ReadEntropy", theoryData);

            try
            {
                var entropy = theoryData.WsTrustSerializer.ReadEntropy(theoryData.Reader, theoryData.WsSerializationContext);
                theoryData.ExpectedException.ProcessNoException(context);
                IdentityComparer.AreEqual(entropy, theoryData.Entropy, context);
            }
            catch (Exception ex)
            {
                theoryData.ExpectedException.ProcessException(ex, context);
            }

            TestUtilities.AssertFailIfErrors(context);
        }

        public static TheoryData<WsTrustSerializerTheoryData> ReadEntropyTheoryData
        {
            get
            {
                var theoryData = new TheoryData<WsTrustSerializerTheoryData>
                {
                    new WsTrustSerializerTheoryData(ReferenceXml.RandomElementReader)
                    {
                        ExpectedException = ExpectedException.ArgumentNullException("serializationContext"),
                        First = true,
                        Reader = ReferenceXml.GetRequestSecurityTokenReader(WsTrustConstants.Trust13, ReferenceXml.Saml2Valid),
                        TestId = "SerializationContextNull",
                        WsSerializationContext = null
                    },
                    new WsTrustSerializerTheoryData(WsTrustVersion.Trust13)
                    {
                        ExpectedException = ExpectedException.ArgumentNullException("reader"),
                        Reader = null,
                        TestId = "ReaderNull",
                    },
                    new WsTrustSerializerTheoryData(WsTrustVersion.Trust13)
                    {
                        ExpectedException = ExpectedException.XmlReadException(),
                        Reader = ReferenceXml.RandomElementReader,
                        TestId = "ReaderNotOnCorrectElement",
                    }
                };

                return theoryData;
            }
        }

        [Theory, MemberData(nameof(ReadLifetimeTheoryData))]
        public void ReadLifetime(WsTrustSerializerTheoryData theoryData)
        {
            var context = TestUtilities.WriteHeader($"{this}.ReadLifetime", theoryData);
            try
            {
                var lifetime = theoryData.WsTrustSerializer.ReadLifetime(theoryData.Reader, theoryData.WsSerializationContext);
                IdentityComparer.AreEqual(lifetime, theoryData.Lifetime, context);
            }
            catch (Exception ex)
            {
                theoryData.ExpectedException.ProcessException(ex, context);
            }

            TestUtilities.AssertFailIfErrors(context);
        }

        public static TheoryData<WsTrustSerializerTheoryData> ReadLifetimeTheoryData
        {
            get
            {
                DateTime created = DateTime.UtcNow;
                DateTime expires = created + TimeSpan.FromDays(1);
                Lifetime lifetime = new Lifetime(created, expires);

                return new TheoryData<WsTrustSerializerTheoryData>
                {
                    new WsTrustSerializerTheoryData(ReferenceXml.RandomElementReader)
                    {
                        ExpectedException = ExpectedException.ArgumentNullException("serializationContext"),
                        First = true,
                        TestId = "SerializationContextNull"
                    },
                    new WsTrustSerializerTheoryData(WsTrustVersion.Trust13)
                    {
                        ExpectedException = ExpectedException.ArgumentNullException("reader"),
                        TestId = "ReaderNull"
                    },
                    new WsTrustSerializerTheoryData(WsTrustVersion.TrustFeb2005)
                    {
                        Lifetime = lifetime,
                        Reader = ReferenceXml.GetLifeTimeReader(WsTrustConstants.TrustFeb2005, created, expires),
                        TestId = "TrustFeb2005"
                    },
                    new WsTrustSerializerTheoryData(WsTrustVersion.Trust13)
                    {
                        Lifetime = lifetime,
                        Reader = ReferenceXml.GetLifeTimeReader(WsTrustConstants.Trust13, created, expires),
                        TestId = "Trust13"
                    },
                    new WsTrustSerializerTheoryData(WsTrustVersion.Trust14)
                    {
                        Lifetime = lifetime,
                        Reader = ReferenceXml.GetLifeTimeReader(WsTrustConstants.Trust14, created, expires),
                        TestId = "Trust14"
                    },
                    new WsTrustSerializerTheoryData(WsTrustVersion.Trust13)
                    {
                        ExpectedException = ExpectedException.XmlReadException("IDX30011:"),
                        Lifetime = lifetime,
                        Reader = ReferenceXml.GetLifeTimeReader(WsTrustConstants.Trust14, created, expires),
                        TestId = "Trust14_13"
                    },
                    new WsTrustSerializerTheoryData(WsTrustVersion.Trust13)
                    {
                        ExpectedException = ExpectedException.XmlReadException("IDX30017:", typeof(FormatException)),
                        Lifetime = lifetime,
                        Reader = ReferenceXml.GetLifeTimeReader(WsTrustConstants.Trust13, XmlConvert.ToString(created, XmlDateTimeSerializationMode.Utc), "xxx"),
                        TestId = "CreateParseError"
                    },
                    new WsTrustSerializerTheoryData(WsTrustVersion.Trust13)
                    {
                        ExpectedException = ExpectedException.XmlReadException("IDX30017:", typeof(FormatException)),
                        Lifetime = lifetime,
                        Reader = ReferenceXml.GetLifeTimeReader(WsTrustConstants.Trust13, "xxx", XmlConvert.ToString(expires, XmlDateTimeSerializationMode.Utc)),
                        TestId = "ExpireParseError"
                    },
                    new WsTrustSerializerTheoryData(WsTrustVersion.Trust13)
                    {
                        ExpectedException = ExpectedException.XmlReadException("IDX30011:"),
                        Lifetime = lifetime,
                        Reader = ReferenceXml.RandomElementReader,
                        TestId = "ReaderNotOnCorrectElement"
                    }
                };
            }
        }

        [Theory, MemberData(nameof(ReadOnBehalfOfTheoryData))]
        public void ReadOnBehalfOf(WsTrustSerializerTheoryData theoryData)
        {
            var context = TestUtilities.WriteHeader($"{this}.ReadOnBehalfOf", theoryData);

            try
            {
                var onBehalfOf = theoryData.WsTrustSerializer.ReadOnBehalfOf(theoryData.Reader, theoryData.WsSerializationContext);
                theoryData.ExpectedException.ProcessNoException(context);
                IdentityComparer.AreEqual(onBehalfOf, theoryData.OnBehalfOf, context);
            }
            catch (Exception ex)
            {
                theoryData.ExpectedException.ProcessException(ex, context);
            }

            TestUtilities.AssertFailIfErrors(context);
        }

        public static TheoryData<WsTrustSerializerTheoryData> ReadOnBehalfOfTheoryData
        {
            get
            {
                var theoryData = new TheoryData<WsTrustSerializerTheoryData>
                {
                    new WsTrustSerializerTheoryData(ReferenceXml.RandomElementReader)
                    {
                        ExpectedException = ExpectedException.ArgumentNullException("serializationContext"),
                        First = true,
                        Reader = ReferenceXml.GetRequestSecurityTokenReader(WsTrustConstants.Trust13, ReferenceXml.Saml2Valid),
                        TestId = "SerializationContextNull",
                        WsSerializationContext = null
                    },
                    new WsTrustSerializerTheoryData(WsTrustVersion.Trust13)
                    {
                        ExpectedException = ExpectedException.ArgumentNullException("reader"),
                        Reader = null,
                        TestId = "ReaderNull",
                    },
                    new WsTrustSerializerTheoryData(WsTrustVersion.Trust13)
                    {
                        ExpectedException = ExpectedException.XmlReadException(),
                        Reader = ReferenceXml.RandomElementReader,
                        TestId = "ReaderNotOnCorrectElement",
                    }
                };

                return theoryData;
            }
        }

        [Theory, MemberData(nameof(ReadRequestTheoryData))]
        public void ReadRequest(WsTrustSerializerTheoryData theoryData)
        {
            var context = TestUtilities.WriteHeader($"{this}.ReadRequest", theoryData);

            try
            {
                var request = theoryData.WsTrustSerializer.ReadRequest(theoryData.Reader);
                theoryData.ExpectedException.ProcessNoException(context);
                IdentityComparer.AreEqual(request, theoryData.WsTrustRequest, context);
            }
            catch (Exception ex)
            {
                theoryData.ExpectedException.ProcessException(ex, context);
            }

            TestUtilities.AssertFailIfErrors(context);
        }

        public static TheoryData<WsTrustSerializerTheoryData> ReadRequestTheoryData
        {
            get
            {
                var theoryData = new TheoryData<WsTrustSerializerTheoryData>
                {
                    new WsTrustSerializerTheoryData(WsTrustVersion.Trust13)
                    {
                        ExpectedException = ExpectedException.ArgumentNullException("reader"),
                        First = true,
                        TestId = "ReaderNull",
                    }
                };

                XmlDictionaryReader reader = ReferenceXml.GetLifeTimeReader(WsTrustConstants.Trust13, DateTime.UtcNow, DateTime.UtcNow + TimeSpan.FromDays(1));
                reader.ReadStartElement();
                reader.ReadStartElement();
                theoryData.Add(new WsTrustSerializerTheoryData(WsTrustVersion.Trust13)
                {
                    ExpectedException = ExpectedException.XmlReadException("IDX30022:"),
                    Reader = reader,
                    TestId = "ReaderNotOnStartElement"
                });

                theoryData.Add(new WsTrustSerializerTheoryData(WsTrustVersion.Trust13)
                {
                    ExpectedException = ExpectedException.XmlReadException("IDX30024:"),
                    Reader = ReferenceXml.RandomElementReader,
                    TestId = "ReaderNotOnCorrectElement"
                });

                return theoryData;
            }
        }

        [Theory, MemberData(nameof(ReadRequestedAttachedReferenceTheoryData))]
        public void ReadRequestedAttachedReference(WsTrustSerializerTheoryData theoryData)
        {
            var context = TestUtilities.WriteHeader($"{this}.ReadRequestedAttachedReference", theoryData);

            try
            {
                var attachedReference = theoryData.WsTrustSerializer.ReadRequestedAttachedReference(theoryData.Reader, theoryData.WsSerializationContext);
                theoryData.ExpectedException.ProcessNoException(context);
                IdentityComparer.AreEqual(attachedReference, theoryData.Reference, context);
            }
            catch (Exception ex)
            {
                theoryData.ExpectedException.ProcessException(ex, context);
            }

            TestUtilities.AssertFailIfErrors(context);
        }

        public static TheoryData<WsTrustSerializerTheoryData> ReadRequestedAttachedReferenceTheoryData
        {
            get
            {
                var theoryData = new TheoryData<WsTrustSerializerTheoryData>
                {
                    new WsTrustSerializerTheoryData(ReferenceXml.RandomElementReader)
                    {
                        ExpectedException = ExpectedException.ArgumentNullException("serializationContext"),
                        First = true,
                        Reader = ReferenceXml.GetRequestSecurityTokenReader(WsTrustConstants.Trust13, ReferenceXml.Saml2Valid),
                        TestId = "SerializationContextNull",
                        WsSerializationContext = null
                    },
                    new WsTrustSerializerTheoryData(WsTrustVersion.Trust13)
                    {
                        ExpectedException = ExpectedException.ArgumentNullException("reader"),
                        Reader = null,
                        TestId = "ReaderNull",
                    },
                    new WsTrustSerializerTheoryData(WsTrustVersion.Trust13)
                    {
                        ExpectedException = ExpectedException.XmlReadException(),
                        Reader = ReferenceXml.RandomElementReader,
                        TestId = "ReaderNotOnCorrectElement",
                    }
                };

                return theoryData;
            }
        }

        [Theory, MemberData(nameof(ReadRequestedProofTokenTheoryData))]
        public void ReadRequestedProofToken(WsTrustSerializerTheoryData theoryData)
        {
            var context = TestUtilities.WriteHeader($"{this}.ReadRequestedProofToken", theoryData);

            try
            {
                var requestedProofToken = theoryData.WsTrustSerializer.ReadRequestedProofToken(theoryData.Reader, theoryData.WsSerializationContext);
                theoryData.ExpectedException.ProcessNoException(context);
                IdentityComparer.AreEqual(requestedProofToken, theoryData.RequestedProofToken, context);
            }
            catch (Exception ex)
            {
                theoryData.ExpectedException.ProcessException(ex, context);
            }

            TestUtilities.AssertFailIfErrors(context);
        }

        public static TheoryData<WsTrustSerializerTheoryData> ReadRequestedProofTokenTheoryData
        {
            get
            {
                var theoryData = new TheoryData<WsTrustSerializerTheoryData>
                {
                    new WsTrustSerializerTheoryData(ReferenceXml.RandomElementReader)
                    {
                        ExpectedException = ExpectedException.ArgumentNullException("serializationContext"),
                        First = true,
                        Reader = ReferenceXml.GetRequestSecurityTokenReader(WsTrustConstants.Trust13, ReferenceXml.Saml2Valid),
                        TestId = "SerializationContextNull",
                        WsSerializationContext = null
                    },
                    new WsTrustSerializerTheoryData(WsTrustVersion.Trust13)
                    {
                        ExpectedException = ExpectedException.ArgumentNullException("reader"),
                        Reader = null,
                        TestId = "ReaderNull",
                    },
                    new WsTrustSerializerTheoryData(WsTrustVersion.Trust13)
                    {
                        ExpectedException = ExpectedException.XmlReadException(),
                        Reader = ReferenceXml.RandomElementReader,
                        TestId = "ReaderNotOnCorrectElement",
                    }
                };

                return theoryData;
            }
        }

        [Theory, MemberData(nameof(ReadRequestedSecurityTokenTheoryData))]
        public void ReadRequestedSecurityToken(WsTrustSerializerTheoryData theoryData)
        {
            var context = TestUtilities.WriteHeader($"{this}.ReadRequestedSecurityToken", theoryData);

            try
            {
                var requestedSecurityToken = theoryData.WsTrustSerializer.ReadRequestedSecurityToken(theoryData.Reader, theoryData.WsSerializationContext);
                theoryData.ExpectedException.ProcessNoException(context);
                IdentityComparer.AreEqual(requestedSecurityToken, theoryData.RequestedSecurityToken, context);
            }
            catch (Exception ex)
            {
                theoryData.ExpectedException.ProcessException(ex, context);
            }

            TestUtilities.AssertFailIfErrors(context);
        }

        public static TheoryData<WsTrustSerializerTheoryData> ReadRequestedSecurityTokenTheoryData
        {
            get
            {
                var theoryData = new TheoryData<WsTrustSerializerTheoryData>
                {
                    new WsTrustSerializerTheoryData(ReferenceXml.RandomElementReader)
                    {
                        ExpectedException = ExpectedException.ArgumentNullException("serializationContext"),
                        First = true,
                        TestId = "SerializationContextNull",
                        WsSerializationContext = null
                    },
                    new WsTrustSerializerTheoryData(WsTrustVersion.Trust13)
                    {
                        ExpectedException = ExpectedException.ArgumentNullException("reader"),
                        Reader = null,
                        TestId = "ReaderNull",
                    },
                    new WsTrustSerializerTheoryData(WsTrustVersion.Trust13)
                    {
                        ExpectedException = ExpectedException.XmlReadException(),
                        Reader = ReferenceXml.RandomElementReader,
                        TestId = "ReaderNotOnCorrectElement",
                    }
                };

                return theoryData;
            }
        }

        [Theory, MemberData(nameof(ReadRequestSeurityTokenResponseTheoryData))]
        public void ReadRequestSeurityTokenResponse(WsTrustSerializerTheoryData theoryData)
        {
            var context = TestUtilities.WriteHeader($"{this}.ReadRequestSeurityTokenResponse", theoryData);

            try
            {
                var requestSecurityTokenResponse = theoryData.WsTrustSerializer.ReadRequestedSecurityToken(theoryData.Reader, theoryData.WsSerializationContext);
                theoryData.ExpectedException.ProcessNoException(context);
                IdentityComparer.AreEqual(requestSecurityTokenResponse, theoryData.RequestedSecurityToken, context);
            }
            catch (Exception ex)
            {
                theoryData.ExpectedException.ProcessException(ex, context);
            }

            TestUtilities.AssertFailIfErrors(context);
        }

        public static TheoryData<WsTrustSerializerTheoryData> ReadRequestSeurityTokenResponseTheoryData
        {
            get
            {
                var theoryData = new TheoryData<WsTrustSerializerTheoryData>
                {
                    new WsTrustSerializerTheoryData(ReferenceXml.RandomElementReader)
                    {
                        ExpectedException = ExpectedException.ArgumentNullException("serializationContext"),
                        First = true,
                        TestId = "SerializationContextNull",
                        WsSerializationContext = null
                    },
                    new WsTrustSerializerTheoryData(WsTrustVersion.Trust13)
                    {
                        ExpectedException = ExpectedException.ArgumentNullException("reader"),
                        Reader = null,
                        TestId = "ReaderNull",
                    },
                    new WsTrustSerializerTheoryData(WsTrustVersion.Trust13)
                    {
                        ExpectedException = ExpectedException.XmlReadException(),
                        Reader = ReferenceXml.RandomElementReader,
                        TestId = "ReaderNotOnCorrectElement",
                    }
                };

                return theoryData;
            }
        }

        [Theory, MemberData(nameof(ReadResponseTheoryData))]
        public void ReadResponse(WsTrustSerializerTheoryData theoryData)
        {
            var context = TestUtilities.WriteHeader($"{this}.ReadResponse", theoryData);

            try
            {
                var response = theoryData.WsTrustSerializer.ReadResponse(theoryData.Reader);
                theoryData.ExpectedException.ProcessNoException(context);
                IdentityComparer.AreEqual(response, theoryData.WsTrustRequest, context);
            }
            catch (Exception ex)
            {
                theoryData.ExpectedException.ProcessException(ex, context);
            }

            TestUtilities.AssertFailIfErrors(context);
        }

        public static TheoryData<WsTrustSerializerTheoryData> ReadResponseTheoryData
        {
            get
            {
                var theoryData = new TheoryData<WsTrustSerializerTheoryData>
                {
                    new WsTrustSerializerTheoryData(WsTrustVersion.Trust13)
                    {
                        ExpectedException = ExpectedException.ArgumentNullException("reader"),
                        First = true,
                        TestId = "ReaderNull",
                    }
                };

                XmlDictionaryReader reader = ReferenceXml.GetLifeTimeReader(WsTrustConstants.Trust13, DateTime.UtcNow, DateTime.UtcNow + TimeSpan.FromDays(1));
                reader.ReadStartElement();
                reader.ReadStartElement();
                theoryData.Add(new WsTrustSerializerTheoryData(WsTrustVersion.Trust13)
                {
                    ExpectedException = ExpectedException.XmlReadException("IDX30022:"),
                    Reader = reader,
                    TestId = "ReaderNotOnStartElement"
                });

                theoryData.Add(new WsTrustSerializerTheoryData(WsTrustVersion.Trust13)
                {
                    ExpectedException = ExpectedException.XmlReadException("IDX30024:"),
                    Reader = ReferenceXml.RandomElementReader,
                    TestId = "ReaderNotOnCorrectElement"
                });

                return theoryData;
            }
        }

        [Theory, MemberData(nameof(ReadUnattachedReferenceTheoryData))]
        public void ReadUnattachedReference(WsTrustSerializerTheoryData theoryData)
        {
            var context = TestUtilities.WriteHeader($"{this}.ReadUnattachedReference", theoryData);

            try
            {
                var unattachedReference = theoryData.WsTrustSerializer.ReadRequestedUnattachedReference(theoryData.Reader, theoryData.WsSerializationContext);
                theoryData.ExpectedException.ProcessNoException(context);
                IdentityComparer.AreEqual(unattachedReference, theoryData.Reference, context);
            }
            catch (Exception ex)
            {
                theoryData.ExpectedException.ProcessException(ex, context);
            }

            TestUtilities.AssertFailIfErrors(context);
        }

        public static TheoryData<WsTrustSerializerTheoryData> ReadUnattachedReferenceTheoryData
        {
            get
            {
                var theoryData = new TheoryData<WsTrustSerializerTheoryData>
                {
                    new WsTrustSerializerTheoryData(ReferenceXml.RandomElementReader)
                    {
                        ExpectedException = ExpectedException.ArgumentNullException("serializationContext"),
                        First = true,
                        TestId = "SerializationContextNull",
                        WsSerializationContext = null
                    },
                    new WsTrustSerializerTheoryData(WsTrustVersion.Trust13)
                    {
                        ExpectedException = ExpectedException.ArgumentNullException("reader"),
                        Reader = null,
                        TestId = "ReaderNull",
                    },
                    new WsTrustSerializerTheoryData(WsTrustVersion.Trust13)
                    {
                        ExpectedException = ExpectedException.XmlReadException(),
                        Reader = ReferenceXml.RandomElementReader,
                        TestId = "ReaderNotOnCorrectElement",
                    }
                };

                return theoryData;
            }
        }

        [Theory, MemberData(nameof(ReadUseKeyTheoryData))]
        public void ReadUseKey(WsTrustSerializerTheoryData theoryData)
        {
            var context = TestUtilities.WriteHeader($"{this}.ReadUseKey", theoryData);

            try
            {
                var useKey = theoryData.WsTrustSerializer.ReadUseKey(theoryData.Reader, theoryData.WsSerializationContext);
                theoryData.ExpectedException.ProcessNoException(context);
                IdentityComparer.AreEqual(useKey, theoryData.UseKey, context);
            }
            catch (Exception ex)
            {
                theoryData.ExpectedException.ProcessException(ex, context);
            }

            TestUtilities.AssertFailIfErrors(context);
        }

        public static TheoryData<WsTrustSerializerTheoryData> ReadUseKeyTheoryData
        {
            get
            {
                var theoryData = new TheoryData<WsTrustSerializerTheoryData>
                {
                    new WsTrustSerializerTheoryData(ReferenceXml.RandomElementReader)
                    {
                        ExpectedException = ExpectedException.ArgumentNullException("serializationContext"),
                        First = true,
                        TestId = "SerializationContextNull",
                        WsSerializationContext = null
                    },
                    new WsTrustSerializerTheoryData(WsTrustVersion.Trust13)
                    {
                        ExpectedException = ExpectedException.ArgumentNullException("reader"),
                        Reader = null,
                        TestId = "ReaderNull",
                    },
                    new WsTrustSerializerTheoryData(WsTrustVersion.Trust13)
                    {
                        ExpectedException = ExpectedException.XmlReadException(),
                        Reader = ReferenceXml.RandomElementReader,
                        TestId = "ReaderNotOnCorrectElement",
                    }
                };

                return theoryData;
            }
        }

        [Theory, MemberData(nameof(WriteBinarySecrectTheoryData))]
        public void WriteBinarySecrect(WsTrustSerializerTheoryData theoryData)
        {
            var context = TestUtilities.WriteHeader($"{this}.WriteBinarySecrect", theoryData);
            try
            {
                theoryData.WsTrustSerializer.WriteBinarySecret(theoryData.Writer, theoryData.WsSerializationContext, theoryData.BinarySecret);
                //IdentityComparer.AreEqual(binarySecret, theoryData.BinarySecret, context);
            }
            catch (Exception ex)
            {
                theoryData.ExpectedException.ProcessException(ex, context);
            }

            TestUtilities.AssertFailIfErrors(context);
        }

        public static TheoryData<WsTrustSerializerTheoryData> WriteBinarySecrectTheoryData
        {
            get
            {
                return new TheoryData<WsTrustSerializerTheoryData>
                {
                    new WsTrustSerializerTheoryData(new MemoryStream())
                    {
                        BinarySecret = new BinarySecret(Convert.FromBase64String(KeyingMaterial.SelfSigned2048_SHA256), WsTrustConstants.Trust13.WsTrustBinarySecretTypes.AsymmetricKey),
                        ExpectedException = ExpectedException.ArgumentNullException("serializationContext"),
                        First = true,
                        TestId = "SerializationContextNull"
                    },
                    new WsTrustSerializerTheoryData(WsTrustVersion.Trust13)
                    {
                        BinarySecret = new BinarySecret(Convert.FromBase64String(KeyingMaterial.SelfSigned2048_SHA256), WsTrustConstants.Trust13.WsTrustBinarySecretTypes.AsymmetricKey),
                        ExpectedException = ExpectedException.ArgumentNullException("writer"),
                        TestId = "WriterNull",
                    },
                    new WsTrustSerializerTheoryData(new MemoryStream(), WsTrustVersion.Trust13)
                    {
                        ExpectedException = ExpectedException.ArgumentNullException("binarySecret"),
                        TestId = "BinarySecretNull"
                    },
                    //new WsTrustSerializerTheoryData(WsTrustVersion.Trust13)
                    //{
                    //    BinarySecret = new BinarySecret(Convert.FromBase64String(KeyingMaterial.SelfSigned2048_SHA256), WsTrustConstants.Trust13.WsTrustBinarySecretTypes.AsymmetricKey),
                    //    Reader = ReferenceXml.GetBinarySecretReader(WsTrustConstants.Trust13, WsTrustConstants.Trust13.WsTrustBinarySecretTypes.AsymmetricKey, KeyingMaterial.SelfSigned2048_SHA256),
                    //    TestId = "Trust13"
                    //},
                    //new WsTrustSerializerTheoryData(WsTrustVersion.Trust14)
                    //{
                    //    BinarySecret = new BinarySecret(Convert.FromBase64String(KeyingMaterial.SelfSigned2048_SHA256), WsTrustConstants.Trust14.WsTrustBinarySecretTypes.AsymmetricKey),
                    //    Reader = ReferenceXml.GetBinarySecretReader(WsTrustConstants.Trust14, WsTrustConstants.Trust14.WsTrustBinarySecretTypes.AsymmetricKey, KeyingMaterial.SelfSigned2048_SHA256),
                    //    TestId = "Trust14"
                    //},
                    //new WsTrustSerializerTheoryData(WsTrustVersion.Trust13)
                    //{
                    //    ExpectedException = new ExpectedException(typeof(XmlReadException), "IDX30017:", typeof(System.Xml.XmlException)),
                    //    BinarySecret = new BinarySecret(Convert.FromBase64String(KeyingMaterial.SelfSigned2048_SHA256), WsTrustConstants.Trust13.WsTrustBinarySecretTypes.AsymmetricKey),
                    //    Reader = ReferenceXml.GetBinarySecretReader(WsTrustConstants.Trust13, WsTrustConstants.Trust13.WsTrustBinarySecretTypes.AsymmetricKey, "xxx"),
                    //    TestId = "EncodingError"
                    //},
                    //new WsTrustSerializerTheoryData(WsTrustVersion.Trust14)
                    //{
                    //    ExpectedException = new ExpectedException(typeof(XmlReadException), "IDX30011:"),
                    //    BinarySecret = new BinarySecret(Convert.FromBase64String(KeyingMaterial.SelfSigned2048_SHA256), WsTrustConstants.Trust13.WsTrustBinarySecretTypes.AsymmetricKey),
                    //    Reader = ReferenceXml.GetBinarySecretReader(WsTrustConstants.Trust13, WsTrustConstants.Trust13.WsTrustBinarySecretTypes.AsymmetricKey, KeyingMaterial.SelfSigned2048_SHA256),
                    //    TestId = "Trust13_14"
                    //},
                    //new WsTrustSerializerTheoryData(WsTrustVersion.Trust13)
                    //{
                    //    ExpectedException = ExpectedException.XmlReadException("IDX30011:"),
                    //    Reader = ReferenceXml.RandomElementReader,
                    //    TestId = "ReaderNotOnCorrectElement"
                    //}
                };
            }
        }

        [Theory, MemberData(nameof(WriteClaimsTheoryData))]
        public void WriteClaims(WsTrustSerializerTheoryData theoryData)
        {
            var context = TestUtilities.WriteHeader($"{this}.WriteClaims", theoryData);
            try
            {
                theoryData.WsTrustSerializer.WriteClaims(theoryData.Writer, theoryData.WsSerializationContext, theoryData.Claims);
                //IdentityComparer.AreEqual(claims, theoryData.Claims, context);
            }
            catch (Exception ex)
            {
                theoryData.ExpectedException.ProcessException(ex, context);
            }

            TestUtilities.AssertFailIfErrors(context);
        }

        public static TheoryData<WsTrustSerializerTheoryData> WriteClaimsTheoryData
        {
            get
            {
                return new TheoryData<WsTrustSerializerTheoryData>
                {
                    new WsTrustSerializerTheoryData(new MemoryStream())
                    {
                        Claims = new Claims("http://ClaimsDialect", new List<ClaimType>()),
                        ExpectedException = ExpectedException.ArgumentNullException("serializationContext"),
                        First = true,
                        TestId = "SerializationContextNull"
                    },
                    new WsTrustSerializerTheoryData(WsTrustVersion.Trust13)
                    {
                        Claims = new Claims("http://ClaimsDialect", new List<ClaimType>()),
                        ExpectedException = ExpectedException.ArgumentNullException("writer"),
                        TestId = "WriterNull",
                    },
                    new WsTrustSerializerTheoryData(new MemoryStream(), WsTrustVersion.Trust13)
                    {
                        ExpectedException = ExpectedException.ArgumentNullException("claims"),
                        TestId = "ClaimsNull"
                    }
                };
            }
        }

        [Theory, MemberData(nameof(WriteEntropyTheoryData))]
        public void WriteEntropy(WsTrustSerializerTheoryData theoryData)
        {
            var context = TestUtilities.WriteHeader($"{this}.WriteEntropy", theoryData);
            try
            {
                theoryData.WsTrustSerializer.WriteEntropy(theoryData.Writer, theoryData.WsSerializationContext, theoryData.Entropy);
                //IdentityComparer.AreEqual(entropy, theoryData.Entropy, context);
            }
            catch (Exception ex)
            {
                theoryData.ExpectedException.ProcessException(ex, context);
            }

            TestUtilities.AssertFailIfErrors(context);
        }

        public static TheoryData<WsTrustSerializerTheoryData> WriteEntropyTheoryData
        {
            get
            {
                return new TheoryData<WsTrustSerializerTheoryData>
                {
                    new WsTrustSerializerTheoryData(new MemoryStream())
                    {
                        Entropy = new Entropy(new BinarySecret(Convert.FromBase64String(KeyingMaterial.SelfSigned2048_SHA256), WsTrustConstants.Trust13.WsTrustBinarySecretTypes.AsymmetricKey)),
                        ExpectedException = ExpectedException.ArgumentNullException("serializationContext"),
                        First = true,
                        TestId = "SerializationContextNull"
                    },
                    new WsTrustSerializerTheoryData(WsTrustVersion.Trust13)
                    {
                        Entropy = new Entropy(new BinarySecret(Convert.FromBase64String(KeyingMaterial.SelfSigned2048_SHA256), WsTrustConstants.Trust13.WsTrustBinarySecretTypes.AsymmetricKey)),
                        ExpectedException = ExpectedException.ArgumentNullException("writer"),
                        TestId = "WriterNull",
                    },
                    new WsTrustSerializerTheoryData(new MemoryStream(), WsTrustVersion.Trust13)
                    {
                        ExpectedException = ExpectedException.ArgumentNullException("entropy"),
                        TestId = "EnthropyNull"
                    }
                };
            }
        }

        [Theory, MemberData(nameof(WriteLifetimeTheoryData))]
        public void WriteLifetime(WsTrustSerializerTheoryData theoryData)
        {
            var context = TestUtilities.WriteHeader($"{this}.WriteLifetime", theoryData);
            try
            {
                theoryData.WsTrustSerializer.WriteLifetime(theoryData.Writer, theoryData.WsSerializationContext, theoryData.Lifetime);
                //IdentityComparer.AreEqual(lifetime, theoryData.Lifetime, context);
            }
            catch (Exception ex)
            {
                theoryData.ExpectedException.ProcessException(ex, context);
            }

            TestUtilities.AssertFailIfErrors(context);
        }

        public static TheoryData<WsTrustSerializerTheoryData> WriteLifetimeTheoryData
        {
            get
            {
                DateTime created = DateTime.UtcNow;
                DateTime expires = created + TimeSpan.FromDays(1);
                Lifetime lifetime = new Lifetime(created, expires);

                return new TheoryData<WsTrustSerializerTheoryData>
                {
                    new WsTrustSerializerTheoryData(new MemoryStream())
                    {
                        Lifetime = lifetime,
                        ExpectedException = ExpectedException.ArgumentNullException("serializationContext"),
                        First = true,
                        TestId = "SerializationContextNull"
                    },
                    new WsTrustSerializerTheoryData(WsTrustVersion.Trust13)
                    {
                        Lifetime = lifetime,
                        ExpectedException = ExpectedException.ArgumentNullException("writer"),
                        TestId = "WriterNull",
                    },
                    new WsTrustSerializerTheoryData(new MemoryStream(), WsTrustVersion.Trust13)
                    {
                        ExpectedException = ExpectedException.ArgumentNullException("lifetime"),
                        TestId = "LifetimeNull"
                    }
                };
            }
        }

        [Theory, MemberData(nameof(WriteOnBehalfOfTheoryData))]
        public void WriteOnBehalfOf(WsTrustSerializerTheoryData theoryData)
        {
            var context = TestUtilities.WriteHeader($"{this}.WriteOnBehalfOf", theoryData);
            try
            {
                theoryData.WsTrustSerializer.WriteOnBehalfOf(theoryData.Writer, theoryData.WsSerializationContext, theoryData.OnBehalfOf);
                //IdentityComparer.AreEqual(lifetime, theoryData.Lifetime, context);
            }
            catch (Exception ex)
            {
                theoryData.ExpectedException.ProcessException(ex, context);
            }

            TestUtilities.AssertFailIfErrors(context);
        }

        public static TheoryData<WsTrustSerializerTheoryData> WriteOnBehalfOfTheoryData
        {
            get
            {
                return new TheoryData<WsTrustSerializerTheoryData>
                {
                    new WsTrustSerializerTheoryData(new MemoryStream())
                    {
                        ExpectedException = ExpectedException.ArgumentNullException("serializationContext"),
                        First = true,
                        OnBehalfOf = new SecurityTokenElement(new SecurityTokenReference()),
                        TestId = "SerializationContextNull"
                    },
                    new WsTrustSerializerTheoryData(WsTrustVersion.Trust13)
                    {
                        ExpectedException = ExpectedException.ArgumentNullException("writer"),
                        OnBehalfOf = new SecurityTokenElement(new SecurityTokenReference()),
                        TestId = "WriterNull",
                    },
                    new WsTrustSerializerTheoryData(new MemoryStream(), WsTrustVersion.Trust13)
                    {
                        ExpectedException = ExpectedException.ArgumentNullException("onBehalfOf"),
                        TestId = "OnBehalfOfNull"
                    }
                };
            }
        }

        [Theory, MemberData(nameof(WriteProofEncryptionTheoryData))]
        public void WriteProofEncryption(WsTrustSerializerTheoryData theoryData)
        {
            var context = TestUtilities.WriteHeader($"{this}.WriteProofEncryption", theoryData);
            try
            {
                theoryData.WsTrustSerializer.WriteProofEncryption(theoryData.Writer, theoryData.WsSerializationContext, theoryData.ProofEncryption);
                //IdentityComparer.AreEqual(lifetime, theoryData.Lifetime, context);
            }
            catch (Exception ex)
            {
                theoryData.ExpectedException.ProcessException(ex, context);
            }

            TestUtilities.AssertFailIfErrors(context);
        }

        public static TheoryData<WsTrustSerializerTheoryData> WriteProofEncryptionTheoryData
        {
            get
            {
                return new TheoryData<WsTrustSerializerTheoryData>
                {
                    new WsTrustSerializerTheoryData(new MemoryStream())
                    {
                        ExpectedException = ExpectedException.ArgumentNullException("serializationContext"),
                        First = true,
                        ProofEncryption = new SecurityTokenElement(new SecurityTokenReference()),
                        TestId = "SerializationContextNull"
                    },
                    new WsTrustSerializerTheoryData(WsTrustVersion.Trust13)
                    {
                        ExpectedException = ExpectedException.ArgumentNullException("writer"),
                        ProofEncryption = new SecurityTokenElement(new SecurityTokenReference()),
                        TestId = "WriterNull",
                    },
                    new WsTrustSerializerTheoryData(new MemoryStream(), WsTrustVersion.Trust13)
                    {
                        ExpectedException = ExpectedException.ArgumentNullException("proofEncryption"),
                        TestId = "ProofEncryptionNull"
                    }
                };
            }
        }

        [Theory, MemberData(nameof(WriteRequestTheoryData))]
        public void WriteRequest(WsTrustSerializerTheoryData theoryData)
        {
            var context = TestUtilities.WriteHeader($"{this}.WriteRequest", theoryData);
            try
            {
                theoryData.WsTrustSerializer.WriteRequest(theoryData.Writer, theoryData.WsTrustVersion, theoryData.WsTrustRequest);
                //IdentityComparer.AreEqual(lifetime, theoryData.Lifetime, context);
            }
            catch (Exception ex)
            {
                theoryData.ExpectedException.ProcessException(ex, context);
            }

            TestUtilities.AssertFailIfErrors(context);
        }

        public static TheoryData<WsTrustSerializerTheoryData> WriteRequestTheoryData
        {
            get
            {
                return new TheoryData<WsTrustSerializerTheoryData>
                {
                    new WsTrustSerializerTheoryData(new MemoryStream())
                    {
                        ExpectedException = ExpectedException.ArgumentNullException("wsTrustVersion"),
                        First = true,
                        TestId = "WsTrustVersionNull",
                        WsTrustRequest = new WsTrustRequest(WsTrustConstants.Trust13.WsTrustActions.Issue),
                    },
                    new WsTrustSerializerTheoryData(WsTrustVersion.Trust13)
                    {
                        ExpectedException = ExpectedException.ArgumentNullException("writer"),
                        TestId = "WriterNull",
                        WsTrustRequest = new WsTrustRequest(WsTrustConstants.Trust13.WsTrustActions.Issue),
                    },
                    new WsTrustSerializerTheoryData(new MemoryStream(), WsTrustVersion.Trust13)
                    {
                        ExpectedException = ExpectedException.ArgumentNullException("trustRequest"),
                        TestId = "WsTrustRequestNull"
                    }
                };
            }
        }

        [Theory, MemberData(nameof(WriteRequestSecurityTokenResponseTheoryData))]
        public void WriteRequestSecurityTokenResponse(WsTrustSerializerTheoryData theoryData)
        {
            var context = TestUtilities.WriteHeader($"{this}.WriteRequestSecurityTokenResponse", theoryData);
            try
            {
                theoryData.WsTrustSerializer.WriteRequestSecurityTokenResponse(theoryData.Writer, theoryData.WsTrustVersion, theoryData.RequestSecurityTokenResponse);
            }
            catch (Exception ex)
            {
                theoryData.ExpectedException.ProcessException(ex, context);
            }

            TestUtilities.AssertFailIfErrors(context);
        }

        public static TheoryData<WsTrustSerializerTheoryData> WriteRequestSecurityTokenResponseTheoryData
        {
            get
            {
                return new TheoryData<WsTrustSerializerTheoryData>
                {
                    new WsTrustSerializerTheoryData(new MemoryStream())
                    {
                        ExpectedException = ExpectedException.ArgumentNullException("wsTrustVersion"),
                        First = true,
                        TestId = "WsTrustVersionNull",
                        WsTrustResponse = new WsTrustResponse(),
                    },
                    new WsTrustSerializerTheoryData(WsTrustVersion.Trust13)
                    {
                        ExpectedException = ExpectedException.ArgumentNullException("writer"),
                        TestId = "WriterNull",
                        WsTrustResponse = new WsTrustResponse(),
                    },
                    new WsTrustSerializerTheoryData(new MemoryStream(), WsTrustVersion.Trust13)
                    {
                        ExpectedException = ExpectedException.ArgumentNullException("requestSecurityTokenResponse"),
                        TestId = "WsTrustResponseNull"
                    }
                };
            }
        }

        [Theory, MemberData(nameof(WriteRequestedAttachedReferenceTheoryData))]
        public void WriteRequestedAttachedReference(WsTrustSerializerTheoryData theoryData)
        {
            var context = TestUtilities.WriteHeader($"{this}.WriteRequestedAttachedReference", theoryData);
            try
            {
                theoryData.WsTrustSerializer.WriteRequestedAttachedReference(theoryData.Writer, theoryData.WsSerializationContext, theoryData.RequestedAttachedReference);
            }
            catch (Exception ex)
            {
                theoryData.ExpectedException.ProcessException(ex, context);
            }

            TestUtilities.AssertFailIfErrors(context);
        }

        public static TheoryData<WsTrustSerializerTheoryData> WriteRequestedAttachedReferenceTheoryData
        {
            get
            {
                return new TheoryData<WsTrustSerializerTheoryData>
                {
                    new WsTrustSerializerTheoryData(new MemoryStream())
                    {
                        ExpectedException = ExpectedException.ArgumentNullException("serializationContext"),
                        First = true,
                        RequestedAttachedReference = new SecurityTokenReference(),
                        TestId = "SerializationContextNull"
                    },
                    new WsTrustSerializerTheoryData(WsTrustVersion.Trust13)
                    {
                        ExpectedException = ExpectedException.ArgumentNullException("writer"),
                        RequestedAttachedReference = new SecurityTokenReference(),
                        TestId = "WriterNull",
                    },
                    new WsTrustSerializerTheoryData(new MemoryStream(), WsTrustVersion.Trust13)
                    {
                        ExpectedException = ExpectedException.ArgumentNullException("securityTokenReference"),
                        TestId = "RequestedAttachedReferenceNull"
                    }
                };
            }
        }

        [Theory, MemberData(nameof(WriteRequestedProofTokenTheoryData))]
        public void WriteRequestedProofToken(WsTrustSerializerTheoryData theoryData)
        {
            var context = TestUtilities.WriteHeader($"{this}.WriteRequestedProofToken", theoryData);
            try
            {
                theoryData.WsTrustSerializer.WriteRequestedProofToken(theoryData.Writer, theoryData.WsSerializationContext, theoryData.RequestedProofToken);
            }
            catch (Exception ex)
            {
                theoryData.ExpectedException.ProcessException(ex, context);
            }

            TestUtilities.AssertFailIfErrors(context);
        }

        public static TheoryData<WsTrustSerializerTheoryData> WriteRequestedProofTokenTheoryData
        {
            get
            {
                return new TheoryData<WsTrustSerializerTheoryData>
                {
                    new WsTrustSerializerTheoryData(new MemoryStream())
                    {
                        ExpectedException = ExpectedException.ArgumentNullException("serializationContext"),
                        First = true,
                        RequestedProofToken = new RequestedProofToken(),
                        TestId = "SerializationContextNull"
                    },
                    new WsTrustSerializerTheoryData(WsTrustVersion.Trust13)
                    {
                        ExpectedException = ExpectedException.ArgumentNullException("writer"),
                        RequestedProofToken = new RequestedProofToken(),
                        TestId = "WriterNull",
                    },
                    new WsTrustSerializerTheoryData(new MemoryStream(), WsTrustVersion.Trust13)
                    {
                        ExpectedException = ExpectedException.ArgumentNullException("requestedProofToken"),
                        TestId = "RequestedProofTokenNull"
                    }
                };
            }
        }

        [Theory, MemberData(nameof(WriteRequestedSecurityTokenTheoryData))]
        public void WriteRequestedSecurityToken(WsTrustSerializerTheoryData theoryData)
        {
            var context = TestUtilities.WriteHeader($"{this}.WriteRequestedSecurityToken", theoryData);
            try
            {
                theoryData.WsTrustSerializer.WriteRequestedSecurityToken(theoryData.Writer, theoryData.WsSerializationContext, theoryData.RequestedSecurityToken);
            }
            catch (Exception ex)
            {
                theoryData.ExpectedException.ProcessException(ex, context);
            }

            TestUtilities.AssertFailIfErrors(context);
        }

        public static TheoryData<WsTrustSerializerTheoryData> WriteRequestedSecurityTokenTheoryData
        {
            get
            {
                return new TheoryData<WsTrustSerializerTheoryData>
                {
                    new WsTrustSerializerTheoryData(new MemoryStream())
                    {
                        ExpectedException = ExpectedException.ArgumentNullException("serializationContext"),
                        First = true,
                        RequestedSecurityToken = new RequestedSecurityToken(),
                        TestId = "SerializationContextNull"
                    },
                    new WsTrustSerializerTheoryData(WsTrustVersion.Trust13)
                    {
                        ExpectedException = ExpectedException.ArgumentNullException("writer"),
                        RequestedSecurityToken = new RequestedSecurityToken(),
                        TestId = "WriterNull",
                    },
                    new WsTrustSerializerTheoryData(new MemoryStream(), WsTrustVersion.Trust13)
                    {
                        ExpectedException = ExpectedException.ArgumentNullException("requestedSecurityToken"),
                        TestId = "RequestedSecurityTokenNull"
                    }
                };
            }
        }

        [Theory, MemberData(nameof(WriteRequestedUnattachedReferenceTheoryData))]
        public void WriteRequestedUnattachedReference(WsTrustSerializerTheoryData theoryData)
        {
            var context = TestUtilities.WriteHeader($"{this}.WriteRequestedUnattachedReference", theoryData);
            try
            {
                theoryData.WsTrustSerializer.WriteRequestedUnattachedReference(theoryData.Writer, theoryData.WsSerializationContext, theoryData.RequestedUnattachedReference);
            }
            catch (Exception ex)
            {
                theoryData.ExpectedException.ProcessException(ex, context);
            }

            TestUtilities.AssertFailIfErrors(context);
        }

        public static TheoryData<WsTrustSerializerTheoryData> WriteRequestedUnattachedReferenceTheoryData
        {
            get
            {
                return new TheoryData<WsTrustSerializerTheoryData>
                {
                    new WsTrustSerializerTheoryData(new MemoryStream())
                    {
                        ExpectedException = ExpectedException.ArgumentNullException("serializationContext"),
                        First = true,
                        RequestedAttachedReference = new SecurityTokenReference(),
                        TestId = "SerializationContextNull"
                    },
                    new WsTrustSerializerTheoryData(WsTrustVersion.Trust13)
                    {
                        ExpectedException = ExpectedException.ArgumentNullException("writer"),
                        RequestedAttachedReference = new SecurityTokenReference(),
                        TestId = "WriterNull",
                    },
                    new WsTrustSerializerTheoryData(new MemoryStream(), WsTrustVersion.Trust13)
                    {
                        ExpectedException = ExpectedException.ArgumentNullException("securityTokenReference"),
                        TestId = "RequestedUnattachedReferenceNull"
                    }
                };
            }
        }

        [Theory, MemberData(nameof(WriteResponseTheoryData))]
        public void WriteResponse(WsTrustSerializerTheoryData theoryData)
        {
            var context = TestUtilities.WriteHeader($"{this}.WriteResponse", theoryData);
            try
            {
                theoryData.WsTrustSerializer.WriteResponse(theoryData.Writer, theoryData.WsTrustVersion, theoryData.WsTrustResponse);
            }
            catch (Exception ex)
            {
                theoryData.ExpectedException.ProcessException(ex, context);
            }

            TestUtilities.AssertFailIfErrors(context);
        }

        public static TheoryData<WsTrustSerializerTheoryData> WriteResponseTheoryData
        {
            get
            {
                return new TheoryData<WsTrustSerializerTheoryData>
                {
                    new WsTrustSerializerTheoryData(new MemoryStream())
                    {
                        ExpectedException = ExpectedException.ArgumentNullException("wsTrustVersion"),
                        First = true,
                        TestId = "WsTrustVersionNull",
                        WsTrustResponse = new WsTrustResponse(),
                    },
                    new WsTrustSerializerTheoryData(WsTrustVersion.Trust13)
                    {
                        ExpectedException = ExpectedException.ArgumentNullException("writer"),
                        TestId = "WriterNull",
                        WsTrustResponse = new WsTrustResponse(),
                    },
                    new WsTrustSerializerTheoryData(new MemoryStream(), WsTrustVersion.Trust13)
                    {
                        ExpectedException = ExpectedException.ArgumentNullException("trustResponse"),
                        TestId = "WsTrustResponseNull"
                    }
                };
            }
        }

        [Theory, MemberData(nameof(WriteUseKeyTheoryData))]
        public void WriteUseKey(WsTrustSerializerTheoryData theoryData)
        {
            var context = TestUtilities.WriteHeader($"{this}.WriteUseKey", theoryData);
            try
            {
                theoryData.WsTrustSerializer.WriteUseKey(theoryData.Writer, theoryData.WsSerializationContext, theoryData.UseKey);
            }
            catch (Exception ex)
            {
                theoryData.ExpectedException.ProcessException(ex, context);
            }

            TestUtilities.AssertFailIfErrors(context);
        }

        public static TheoryData<WsTrustSerializerTheoryData> WriteUseKeyTheoryData
        {
            get
            {
                return new TheoryData<WsTrustSerializerTheoryData>
                {
                    new WsTrustSerializerTheoryData(new MemoryStream())
                    {
                        ExpectedException = ExpectedException.ArgumentNullException("serializationContext"),
                        First = true,
                        UseKey = new UseKey(new SecurityTokenElement(new SecurityTokenReference())),
                        TestId = "SerializationContextNull"
                    },
                    new WsTrustSerializerTheoryData(WsTrustVersion.Trust13)
                    {
                        ExpectedException = ExpectedException.ArgumentNullException("writer"),
                        UseKey = new UseKey(new SecurityTokenElement(new SecurityTokenReference())),
                        TestId = "WriterNull",
                    },
                    new WsTrustSerializerTheoryData(new MemoryStream(), WsTrustVersion.Trust13)
                    {
                        ExpectedException = ExpectedException.ArgumentNullException("useKey"),
                        TestId = "UseKeyNull"
                    }
                };
            }
        }
    }

    public class WsTrustSerializerTheoryData : TheoryDataBase
    {
        public WsTrustSerializerTheoryData() { }

        public WsTrustSerializerTheoryData(WsTrustVersion trustVersion)
        {
            WsSerializationContext = new WsSerializationContext(trustVersion);
            WsTrustVersion = trustVersion;
        }

        public WsTrustSerializerTheoryData(XmlDictionaryReader reader)
        {
            Reader = reader;
        }

        public WsTrustSerializerTheoryData(MemoryStream memoryStream)
        {
            MemoryStream = memoryStream;
            Writer = XmlDictionaryWriter.CreateTextWriter(memoryStream, Encoding.UTF8);
        }

        public WsTrustSerializerTheoryData(MemoryStream memoryStream, WsTrustVersion trustVersion)
        {
            MemoryStream = memoryStream;
            Writer = XmlDictionaryWriter.CreateTextWriter(memoryStream, Encoding.UTF8);
            WsSerializationContext = new WsSerializationContext(trustVersion);
            WsTrustVersion = trustVersion;
        }

        public BinarySecret BinarySecret { get; set; }

        public Claims Claims { get; set; }

        public Entropy Entropy { get; set; }

        public Lifetime Lifetime { get; set; }

        public MemoryStream MemoryStream { get; set; }

        public SecurityTokenElement OnBehalfOf { get; set; }

        public SecurityTokenElement ProofEncryption { get; set; }

        public XmlDictionaryReader Reader { get; set; }

        public SecurityTokenReference Reference { get; set; }

        public SecurityTokenReference RequestedAttachedReference { get; set; }

        public RequestedProofToken RequestedProofToken { get; set; }

        public RequestedSecurityToken RequestedSecurityToken { get; set; }

        public SecurityTokenReference RequestedUnattachedReference { get; set; }

        public RequestSecurityTokenResponse RequestSecurityTokenResponse { get; set; }

        public UseKey UseKey { get; set; }

        public XmlDictionaryWriter Writer { get; set; }

        public WsSerializationContext WsSerializationContext { get; set; }

        public WsTrustRequest WsTrustRequest { get; set; }

        public WsTrustResponse WsTrustResponse { get; set; }

        public WsTrustSerializer WsTrustSerializer { get; set; } = new WsTrustSerializer();

        public WsTrustVersion WsTrustVersion { get; set; }
    }
}