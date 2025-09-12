import os
import base64

from typing import Iterable, List
from cryptography.hazmat.primitives.ciphers import Cipher, algorithms, modes
from cryptography.hazmat.primitives.padding import PKCS7
from cryptography.hazmat.backends import default_backend    
from temporalio.api.common.v1 import Payload
from temporalio.converter import PayloadCodec

default_key = b"test-key-test-key-test-key-test!"
default_key_id = "enc-key-1"
encoding_str = "binary/encrypted"
encoding_byte_str = encoding_str.encode('utf-8')

class EncryptionCodec(PayloadCodec):
    def __init__(self, key_id: str = default_key_id, key: bytes = default_key) -> None:
        super().__init__()
        self.key_id = key_id
        self.key = key

    async def encode(self, payloads: Iterable[Payload]) -> List[Payload]:
        return [
            Payload(
                metadata={
                    "encoding": encoding_byte_str,
                    "encryption-key-id": self.key_id.encode(),
                    },
                data=self.encrypt(p.SerializeToString()),
            )
            for p in payloads
        ]

    async def decode(self, payloads: Iterable[Payload]) -> List[Payload]:
        ret: List[Payload] = []
        for p in payloads:
            if p.metadata.get("encoding", b"").decode() != encoding_str:
                ret.append(p)
                continue

            key_id = p.metadata.get("encryption-key-id", b"").decode()
            if key_id != self.key_id:
                raise ValueError(
                    f"Unrecognized key ID {key_id}. Current key ID is {self.key_id}."
                    )
            ret.append(Payload.FromString(self.decrypt(p.data)))
        return ret

    def encrypt(self, data: bytes) -> bytes:
        iv = os.urandom(16)  # Generate a random IV
        cipher = Cipher(algorithms.AES(self.key), modes.CBC(iv), backend=default_backend())
        encryptor = cipher.encryptor()
        padder = PKCS7(algorithms.AES.block_size).padder()
        padded_data = padder.update(data) + padder.finalize()
        return iv + encryptor.update(padded_data) + encryptor.finalize()

    def decrypt(self, ciphertext: bytes) -> bytes:
        """Decrypt data produced by :meth:`encrypt`.

        Expects `ciphertext` to be IV (16 bytes) + AES-CBC ciphertext.
        """
        if not isinstance(ciphertext, (bytes, bytearray)):
            raise TypeError("ciphertext must be bytes")
        if len(ciphertext) < 16:
            raise ValueError("Ciphertext too short, missing IV or ciphertext")

        iv = bytes(ciphertext[:16])  # Extract the IV from the beginning
        actual_ct = bytes(ciphertext[16:])  # The actual ciphertext follows the IV
        cipher = Cipher(algorithms.AES(self.key), modes.CBC(iv), backend=default_backend())
        decryptor = cipher.decryptor()
        padded_data = decryptor.update(actual_ct) + decryptor.finalize()
        unpadder = PKCS7(algorithms.AES.block_size).unpadder()
        plaintext = unpadder.update(padded_data) + unpadder.finalize()
        return plaintext


        
        
        
