# platform-user-core-func
## User Service
This is the header
## Messages
> # user:get
## *Request*
```
{}
```

### Example - Direct
```
{
  "topic": "user:get",
  "message": "{}"
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
}[]
```
&nbsp;

---
&nbsp;
