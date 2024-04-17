# platform-tenant-core-func
## Tenant Service
This is the header
## Messages
> # tenant:get
## *Request*
```
{
    id: guid
}
```
### Validation
| **Field** | **Validation** |
| - | - |
|id|Not Null|

### Example - Direct
```
{
  "topic": "tenant:get",
  "message": "{\"id\":\"11111111-1111-1111-1111-111111111111\"}"
}
```

## *Responses*
> ## tenant:get:result
*Response to the sender*
```
{
    id: guid
    name: string
    crn: string
    internal: {
        value1: string
        value2: {...}
    }
}
```
&nbsp;

---
&nbsp;
