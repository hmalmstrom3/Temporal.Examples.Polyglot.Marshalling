import asyncio
import pytest

from temporalio.api.common.v1 import Payload
from codec.codec import EncryptionCodec, default_key_id, default_key, encoding_str


def test_encrypt_decrypt_roundtrip():
    codec = EncryptionCodec(key_id=default_key_id, key=default_key)
    plaintext = b"hello world"
    ciphertext = codec.encrypt(plaintext)
    assert isinstance(ciphertext, (bytes, bytearray))
    # ciphertext should be longer than plaintext because of IV + padding
    assert len(ciphertext) >= len(plaintext) + 16
    recovered = codec.decrypt(ciphertext)
    assert recovered == plaintext


@pytest.mark.asyncio
async def test_encode_decode_roundtrip():
    codec = EncryptionCodec(key_id=default_key_id, key=default_key)
    # Create a Payload that simulates a Temporal payload (use Payload with raw data)
    p = Payload(metadata={}, data=b"some-bytes")
    encoded_list = await codec.encode([p])
    assert len(encoded_list) == 1
    enc_p = encoded_list[0]
    assert enc_p.metadata.get(b"encoding") is not None or enc_p.metadata.get("encoding") is not None
    # decode should return the original payload inside a list
    decoded = await codec.decode(encoded_list)
    assert len(decoded) == 1
    # Decoded payload is a Payload object serialized back by Payload.FromString
    # Since we constructed a raw Payload earlier (not a serialized), encode used p.SerializeToString(),
    # so now we expect decode to return that original serialized data unpacked into a Payload
    assert isinstance(decoded[0], Payload)
    # The decoded payload data should match original data
    assert decoded[0].SerializeToString() == p.SerializeToString()
