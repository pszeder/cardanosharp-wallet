﻿
using System;
using CardanoSharp.Wallet.Common;
using CardanoSharp.Wallet.Enums;
using CardanoSharp.Wallet.Models.Addresses;
using CardanoSharp.Wallet.Models.Keys;
using CardanoSharp.Wallet.Utilities;

namespace CardanoSharp.Wallet
{
    public interface IAddressService
    {
        [Obsolete]
        Address GetAddress(PublicKey payment, PublicKey stake, NetworkType networkType, AddressType addressType);
        Address GetBaseAddress(PublicKey payment, PublicKey stake, NetworkType networkType);
        Address GetRewardAddress(PublicKey stake, NetworkType networkType);
        Address GetEnterpriseAddress(PublicKey payment, NetworkType networkType);
        Address ExtractRewardAddress(Address basePaymentAddress);
    }
    public class AddressService : IAddressService
    {
        [Obsolete("This method has been broken up. Please see other GetAddress methods.")]
        public Address GetAddress(PublicKey payment, PublicKey stake, NetworkType networkType, AddressType addressType)
        {
            var networkInfo = getNetworkInfo(networkType);
            var paymentEncoded = HashUtility.Blake2b224(payment.Key);
            var stakeEncoded = HashUtility.Blake2b224(stake.Key);

            //get prefix
            var prefix = $"{GetPrefixHeader(addressType)}{GetPrefixTail(networkType)}";

            //get header
            var header = getAddressHeader(networkInfo, addressType);
            //get body
            byte[] addressArray;
            switch (addressType)
            {
                case AddressType.Base:
                    addressArray = new byte[1 + paymentEncoded.Length + stakeEncoded.Length];
                    addressArray[0] = header;
                    Buffer.BlockCopy(paymentEncoded, 0, addressArray, 1, paymentEncoded.Length);
                    Buffer.BlockCopy(stakeEncoded, 0, addressArray, paymentEncoded.Length + 1, stakeEncoded.Length);
                    break;
                case AddressType.Enterprise:
                    addressArray = new byte[1 + paymentEncoded.Length];
                    addressArray[0] = header;
                    Buffer.BlockCopy(paymentEncoded, 0, addressArray, 1, paymentEncoded.Length);
                    break;
                case AddressType.Reward:
                    addressArray = new byte[1 + stakeEncoded.Length];
                    addressArray[0] = header;
                    Buffer.BlockCopy(stakeEncoded, 0, addressArray, 1, stakeEncoded.Length);
                    break;
                default:
                    throw new Exception("Unknown address type");
            }

            return new Address(prefix, addressArray);
        }

        public Address GetBaseAddress(PublicKey payment, PublicKey stake, NetworkType networkType)
        {
            var addressType = AddressType.Base;
            var networkInfo = getNetworkInfo(networkType);
            var paymentEncoded = HashUtility.Blake2b224(payment.Key);
            var stakeEncoded = HashUtility.Blake2b224(stake.Key);

            //get prefix
            var prefix = $"{GetPrefixHeader(addressType)}{GetPrefixTail(networkType)}";

            //get header
            var header = getAddressHeader(networkInfo, addressType);
            
            //get body
            byte[] addressArray = new byte[1 + paymentEncoded.Length + stakeEncoded.Length];
            addressArray[0] = header;
            Buffer.BlockCopy(paymentEncoded, 0, addressArray, 1, paymentEncoded.Length);
            Buffer.BlockCopy(stakeEncoded, 0, addressArray, paymentEncoded.Length + 1, stakeEncoded.Length);

            return new Address(prefix, addressArray);
        }

        public Address GetRewardAddress(PublicKey stake, NetworkType networkType)
        {
            var addressType = AddressType.Reward;
            var networkInfo = getNetworkInfo(networkType);
            var stakeEncoded = HashUtility.Blake2b224(stake.Key);

            //get prefix
            var prefix = $"{GetPrefixHeader(addressType)}{GetPrefixTail(networkType)}";

            //get header
            var header = getAddressHeader(networkInfo, addressType);
            
            //get body
            byte[] addressArray = new byte[1 + stakeEncoded.Length];
            addressArray[0] = header;
            Buffer.BlockCopy(stakeEncoded, 0, addressArray, 1, stakeEncoded.Length);

            return new Address(prefix, addressArray);
        }

        public Address GetEnterpriseAddress(PublicKey payment, NetworkType networkType)
        {
            var addressType = AddressType.Enterprise;
            var networkInfo = getNetworkInfo(networkType);
            var paymentEncoded = HashUtility.Blake2b224(payment.Key);

            //get prefix
            var prefix = $"{GetPrefixHeader(addressType)}{GetPrefixTail(networkType)}";

            //get header
            var header = getAddressHeader(networkInfo, addressType);
            
            //get body
            byte[] addressArray = new byte[1 + paymentEncoded.Length];
            addressArray[0] = header;
            Buffer.BlockCopy(paymentEncoded, 0, addressArray, 1, paymentEncoded.Length);

            return new Address(prefix, addressArray);
        }

        public Address ExtractRewardAddress(Address basePaymentAddress)
        {
            if (basePaymentAddress.AddressType != AddressType.Base)
                throw new ArgumentException($"{nameof(basePaymentAddress)}:{basePaymentAddress} is not a base address", nameof(basePaymentAddress));

            // The stake key digest is the second half of a base address's bytes (pre-bech32)
            // and same value as the blake2b-224 hash digest of the stake key (blake2b-224=224bits=28bytes)
            const int stakeKeyDigestByteLength = 28;
            byte[] rewardAddressBytes = new byte[1 + stakeKeyDigestByteLength];
            var rewardAddressPrefix = $"{GetPrefixHeader(AddressType.Reward)}{GetPrefixTail(basePaymentAddress.NetworkType)}";
            var rewardAddressHeader = getAddressHeader(getNetworkInfo(basePaymentAddress.NetworkType), AddressType.Reward);
            rewardAddressBytes[0] = rewardAddressHeader;
            // Extract stake key hash from baseAddressBytes 
            Buffer.BlockCopy(basePaymentAddress.GetBytes(), 29, rewardAddressBytes, 1, stakeKeyDigestByteLength);

            return new Address(rewardAddressPrefix, rewardAddressBytes);
        }

        public static string GetPrefix(AddressType addressType, NetworkType networkType) =>
            $"{GetPrefixHeader(addressType)}{GetPrefixTail(networkType)}";

        public static string GetPrefixHeader(AddressType addressType) =>
            addressType switch
            {
                AddressType.Reward => "stake",
                AddressType.Base => "addr",
                AddressType.Enterprise => "addr",
                _ => throw new Exception("Unknown address type")
            };

        public static string GetPrefixTail(NetworkType networkType) =>
            networkType switch
            {
                NetworkType.Testnet => "_test",
                NetworkType.Mainnet => "",
                _ => throw new Exception("Unknown address type")
            };

        private NetworkInfo getNetworkInfo(NetworkType type) =>
            type switch
            {
                NetworkType.Testnet => new NetworkInfo(0b0000, 1097911063),
                NetworkType.Mainnet => new NetworkInfo(0b0001, 764824073),
                _ => throw new Exception("Unknown network type")
            };

        private byte getAddressHeader(NetworkInfo networkInfo, AddressType addressType) =>
            addressType switch
            {
                AddressType.Base => (byte)(networkInfo.NetworkId & 0xF),
                AddressType.Enterprise => (byte)(0b0110_0000 | networkInfo.NetworkId & 0xF),
                AddressType.Reward => (byte)(0b1110_0000 | networkInfo.NetworkId & 0xF),
                _ => throw new Exception("Unknown address type")
            };
    }
}
