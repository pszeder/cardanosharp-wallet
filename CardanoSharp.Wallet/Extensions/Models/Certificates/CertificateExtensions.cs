﻿using CardanoSharp.Wallet.Models.Transactions;
using PeterO.Cbor2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CardanoSharp.Wallet.Extensions.Models.Certificates
{
    public static class CertificateExtensions
    {
        public static CBORObject GetCBOR(this Certificate certificate)
        {
            /*
            certificate =
            [stake_registration
            // stake_deregistration
            // stake_delegation
            // pool_registration
            // pool_retirement
            // genesis_key_delegation
            // move_instantaneous_rewards_cert
            ]

            stake_registration = (0, stake_credential)
            stake_deregistration = (1, stake_credential)
            stake_delegation = (2, stake_credential, pool_keyhash)
            pool_registration = (3, pool_params)
            pool_retirement = (4, pool_keyhash, epoch)
            genesis_key_delegation = (5, genesishash, genesis_delegate_hash, vrf_keyhash)
            move_instantaneous_rewards_cert = (6, move_instantaneous_reward)*/
            //Certificates are byte[] but we may need to denote type...
            //add a new Certificate

            var cborCert = CBORObject.NewArray();

            if (certificate.StakeRegistration != null)
            {
                cborCert.Add(CBORObject.NewArray()
                    .Add(0)
                    .Add(CBORObject.NewArray()
                        .Add(0)
                        .Add(certificate.StakeRegistration)
                    ));
            }

            if (certificate.StakeDeregistration != null)
            {
                cborCert.Add(CBORObject.NewArray()
                    .Add(1)
                    .Add(CBORObject.NewArray()
                        .Add(0)
                        .Add(certificate.StakeDeregistration)
                    ));
            }

            if (certificate.StakeDelegation != null)
            {
                cborCert.Add(certificate.StakeDelegation.GetCBOR());
            }

            //if (certificate.PoolRegistration != null)
            //{

            //}

            //if (certificate.PoolRetirement != null)
            //{

            //}

            //if (certificate.GenesisKeyDelegation != null)
            //{

            //}

            //if (certificate.GenesisKeyDelegation != null)
            //{

            //}

            return cborCert;
        }

        public static Certificate GetCertificate(this CBORObject certificateCbor)
        {
            //validation
            if (certificateCbor == null)
            {
                throw new ArgumentNullException(nameof(certificateCbor));
            }
            if (certificateCbor.Type != CBORType.Array)
            {
                throw new ArgumentException("certificateCbor is not expected type CBORType.Array");
            }

            //get data
            var certificate = new Certificate();

            foreach (var certItem in certificateCbor.Values)
            {
                //should always be an array
                if (certItem.Type != CBORType.Array)
                {
                    throw new ArgumentException("certificateCbor array item is not expected type CBORType.Array");
                }

                if (!certItem.Values.First().IsNumber)
                {
                    throw new ArgumentException("certificateCbor array item has invalid first element (expected number)");
                }
                var index = Convert.ToInt32(certItem.Values.First().DecodeValueByCborType());
                switch (index)
                {
                    case 0: //stake registration
                        var regCert = ((string)certItem.Values.Skip(1).First().DecodeValueByCborType()).HexToByteArray();
                        certificate.StakeRegistration = regCert;
                        break;
                    case 1: //stake deregistration
                        var deregCert = ((string)certItem.Values.Skip(1).First().DecodeValueByCborType()).HexToByteArray();
                        certificate.StakeDeregistration = deregCert;
                        break;
                    case 2: //stake delegation
                        certificate.StakeDelegation = certItem.GetStakeDelegation();
                        break;
                    default: throw new ArgumentException("certificateCbor array item had unexpected index value (expected 0 or 1)");
                }
            }

            //return
            return certificate;
        }

        public static byte[] Serialize(this Certificate certificate)
        {
            return certificate.GetCBOR().EncodeToBytes();
        }
    }
}
