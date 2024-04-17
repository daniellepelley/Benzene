# platform-user-core-func
## User Service
This is the header
## Messages
> # user:get
## *Request*
```
{
    id: string
}
```
### Validation
| **Field** | **Validation** |
| - | - |
|id|Not Null, Maximum Length of 10 characters|

### Example - Direct
```
{
  "topic": "user:get",
  "message": "{\"id\":\"value\"}"
}
```

## *Responses*
> ## user:get:result
*Response to the sender*
```
{
    id: string
    tenantIds: string[]
    internal: {
        value1: string
        value2: {...}
    }
}
```
&nbsp;

---
&nbsp;
