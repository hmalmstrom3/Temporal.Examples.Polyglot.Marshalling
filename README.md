# Example Temporal Polyglot usage

## Cross language interaction with more complex data parameters / return types than the Temporal examples have

Prerequisites: 

Temporal CLI, a running local temporal and respective language dependencies.

### .Net workflow / worker with python activity / worker

In this scenario we use a .net workflow that marshalls an object to a python Activity using a parameter that is a Pydantic model.
The result of that activity is in turn marshalled back to .Net.

Some things to note: 
- Json deserialization occurs in pydantic library based on the temporal queue message, if that structure is different than expected you may need to use aliases for marshalling it correctly.
- Optional fields are likely going to be needed if you want the ability to call without arguments or with arguments defaulted.
- This is without codec SDK

## TODO: Data Converters experiments

## TODO: Integrate Codec Server to encrypt fields

