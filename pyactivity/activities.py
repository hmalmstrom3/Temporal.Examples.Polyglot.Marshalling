from temporalio import activity
from pydantic import BaseModel, Field
from typing import Dict, Optional
from datetime import datetime

class ActivityInputData(BaseModel):
    input1: str = Field(alias='Input1')
    isEnabled: bool = Field(alias='IsEnabled', default=False)
    count: int = Field(alias='Count')
    arrItems: list[str] = Field(alias='ArrItems')
    listItems: list[str] = Field(alias='ListItems')
    dictItems: Dict[str, str] = Field(alias='DictItems')
    timestamp: datetime = Field(alias='Timestamp')

    class Config:
        allow_population_by_field_name = True


class ReturnData(BaseModel):
    result: Optional[str] = Field(alias='Result', default="")
    success: bool = Field(alias='Success', default=False)
    processedAt: datetime = Field(alias='ProcessedAt')
    

@activity.defn
async def PythonProcessDataAsync(inputData: ActivityInputData) -> ReturnData:
    r = f"""
    We received an object that looks like this:
      input1: {inputData.input1}
      isEnabled: {inputData.isEnabled}
      count: {inputData.count}
      arrItems: {", ".join(inputData.arrItems)}
      listItems: {", ".join(inputData.listItems)}
      dictItems: {inputData.dictItems.items()}
      timestamp: {inputData.timestamp}
    """
    return ReturnData(Result=r, Success=True, ProcessedAt=datetime.now())


    
      
