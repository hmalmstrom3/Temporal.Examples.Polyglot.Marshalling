import asyncio
import logging
import sys
import os
import dataclasses

from temporalio.converter import DefaultPayloadConverter, DataConverter
from temporalio.client import Client
from temporalio.worker import Worker
from temporalio.contrib.pydantic import PydanticPayloadConverter

from activities import PythonProcessDataAsync

# hack for loading my module for now until we push it to artifactory
module_dir = '../pyEncryptionCodec/codec'
sys.path.append(module_dir)
import codec
from codec import EncryptionCodec

task_queue = "python-task-queue"

our_data_converter = DataConverter (
    payload_converter_class=PydanticPayloadConverter,
    payload_codec=EncryptionCodec(),
)
async def main() -> None:
    print("Initializing Temporal Py Worker")
    client = await Client.connect(
        "localhost:7233",
        data_converter=our_data_converter
    )
    worker = Worker(
        client,
        task_queue=task_queue,
        activities=[
            PythonProcessDataAsync
        ]
    )
    print("Starting Temporal Py Worker")
    await worker.run()
    print("Shutting Py Worker down.")
        

if __name__ == "__main__":
    asyncio.run(main())
