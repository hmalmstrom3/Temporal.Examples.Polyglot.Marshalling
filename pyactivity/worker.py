import asyncio
import logging

from temporalio.client import Client
from temporalio.worker import Worker
from temporalio.contrib.pydantic import pydantic_data_converter

from activities import PythonProcessDataAsync

task_queue = "python-task-queue"

async def main() -> None:
    print("Initializing Temporal Py Worker")
    client = await Client.connect(
        "localhost:7233",
        data_converter=pydantic_data_converter
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
