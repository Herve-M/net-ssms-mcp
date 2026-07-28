---
description: "REST API guidelines"
paths:
    [
        "**/src/Api.Server/Controllers/V*/*.cs",
        "**/src/Api.Server/Models/API/V*/*.cs",
    ]
---

# REST API rules

## Version scoping

REST API are versionized using `Asp.Versioning` package, and each controller should have an `ApiVersion` attrinute set.

Example for 1.0, stored under `src/Api.Server/Controllers/V1/`:

```csharp
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public sealed class ServersController {}
```

Controller are stored within a version folder, following format `Vx` under `src/Api.Server/Controllers/`.

Example:

- For 1.0, stored under `src/Api.Server/Controllers/V1/`
- For 2.0, stored under `src/Api.Server/Controllers/V2/`

## API Guidance

### HTTP Methods

| HTTP Method | Description                                                                |
| ----------- | -------------------------------------------------------------------------- |
| `GET`       | To _retrieve_ a resource.                                                  |
| `POST`      | To _create_ a resource, or to _execute_ a complex operation on a resource. |
| `PUT`       | To _update_ a resource.                                                    |
| `DELETE`    | To _delete_ a resource.                                                    |
| `PATCH`     | To perform a _partial update_ to a resource.                               |

The actual operation invoked MUST match the HTTP method semantics as defined in the table above.

- The **`GET`** method MUST NOT have side effects. It MUST NOT change the state of an underlying resource.
- **`POST`**: method SHOULD be used to create a new resource in a collection.
    - **Example:** To add a credit card on file, `POST https://api.foo.com/v1/vault/credit-cards`
    - Idempotency semantics: If this is a subsequent execution of the same invocation (including the [`Foo-Request-Id`](#http-custom-headers) header) and the resource was already created, then the request SHOULD be idempotent.
- The **`POST`** method SHOULD be used to create a new sub-resource and establish its relationship with the main resource.
    - **Example:** To refund a payment with transaction ID 12345: `POST https://api.foo.com/v1/payments/payments/12345/refund`
- The **`POST`** method MAY be used in complex operations, along with the name of the operation. This is also known as the _controller pattern_ and is considered an exception to the RESTful model. It is more applicable in cases when resources represent a business process, and operations are the steps or actions to be performed as part of it. For more information, please refer to [section 2.6](http://techbus.safaribooksonline.com/9780596809140/chapter-identifying-resources) of the [RESTful Web Services Cookbook][29].
- The **`PUT`** method SHOULD be used to update resource attributes or to establish a relationship from a resource to an existing sub-resource; it updates the main resource with a reference to the sub-resource.

### Return code

All REST APIs MUST use only the following status codes. Status codes in **`BOLD`** SHOULD be used by API developers. The rest are primarily intended for web-services framework developers reporting framework-level errors related to security, content negotiation, etc.

- APIs MUST NOT return a status code that is not defined in this table.
- APIs MAY return only some of status codes defined in this table.

| Status Code                     | Description                                                                                                                                                                                                                                                                                                                                   |
| ------------------------------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| **`200 OK`**                    | Generic successful execution.                                                                                                                                                                                                                                                                                                                 |
| **`201 Created`**               | Used as a response to `POST` method execution to indicate successful creation of a resource. If the resource was already created (by a previous execution of the same method, for example), then the server should return status code `200 OK`.                                                                                               |
| **`202 Accepted`**              | Used for asynchronous method execution to specify the server has accepted the request and will execute it at a later time. For more details, please refer [Asynchronous Operations](patterns.md#asynchronous-operations).                                                                                                                     |
| **`204 No Content`**            | The server has successfully executed the method, but there is no entity body to return.                                                                                                                                                                                                                                                       |
| **`400 Bad Request`**           | The request could not be understood by the server. Use this status code to specify:<br/> <ul><li>The data as part of the payload cannot be converted to the underlying data type.</li><li>The data is not in the expected data format.</li><li>Required field is not available.</li><li>Simple data validation type of error.</li></ul>       |
| `401 Unauthorized`              | The request requires authentication and none was provided. Note the difference between this and `403 Forbidden`.                                                                                                                                                                                                                              |
| **`403 Forbidden`**             | The client is not authorized to access the resource, although it may have valid credentials. API could use this code in case business level authorization fails. For example, accound holder does not have enough funds.                                                                                                                      |
| **`404 Not Found`**             | The server has not found anything matching the request URI. This either means that the URI is incorrect or the resource is not available. For example, it may be that no data exists in the database at that key.                                                                                                                             |
| `405 Method Not Allowed`        | The server has not implemented the requested HTTP method. This is typically default behavior for API frameworks.                                                                                                                                                                                                                              |
| `406 Not Acceptable`            | The server MUST return this status code when it cannot return the payload of the response using the media type requested by the client. For example, if the client sends an `Accept: application/xml` header, and the API can only generate `application/json`, the server MUST return `406`.                                                 |
| `415 Unsupported Media Type`    | The server MUST return this status code when the media type of the request's payload cannot be processed. For example, if the client sends a `Content-Type: application/xml` header, but the API can only accept `application/json`, the server MUST return `415`.                                                                            |
| **`422 Unprocessable Entity`**  | The requested action cannot be performed and may require interaction with APIs or processes outside of the current request. This is distinct from a 500 response in that there are no systemic problems limiting the API from performing the request.                                                                                         |
| `429 Too Many Requests`         | The server must return this status code if the rate limit for the user, the application, or the token has exceeded a predefined value. Defined in Additional HTTP Status Codes [RFC 6585](https://tools.ietf.org/html/rfc6585).                                                                                                               |
| **`500 Internal Server Error`** | This is either a system or application error, and generally indicates that although the client appeared to provide a correct request, something unexpected has gone wrong on the server. A `500` response indicates a server-side software defect or site outage. `500` SHOULD NOT be utilized for client validation or logic error handling. |
| `503 Service Unavailable`       | The server is unable to handle the request for a service due to temporary maintenance.                                                                                                                                                                                                                                                        |

| Status Code | 200 Success | 201 Created | 202 Accepted | 204 No Content | 400 Bad Request | 404 Not Found | 422 Unprocessable Entity | 500 Internal Server Error |
| ----------- | :---------- | :---------- | :----------- | :------------- | :-------------- | :------------ | :----------------------- | :------------------------ |
| `GET`       | X           |             |              |                | X               | X             | **`X`**                  | X                         |
| `POST`      | X           | X           | **`X`**      |                | X               | **`X`**       | **`X`**                  | X                         |
| `PUT`       | X           |             | **`X`**      | X              | X               | X             | **`X`**                  | X                         |
| `PATCH`     | X           |             |              | X              | X               | X             | **`X`**                  | X                         |
| `DELETE`    | X           |             |              | X              | X               | X             | **`X`**                  | X                         |

### Request / Response pattern

All controller actions MUST declare every possible HTTP response code using `[ProducesResponseType]` attributes.

All models, input or output, should be named as follow except for Id input:

- Input: `Request`
- Output: `Response`

Example for `POST Database/AddDatabaseServer`:

- Input name: `DatabaseServerAddRequest`
- Output name: `DatabaseServerDetailsResponse`

Model DTO are stored within a version folder, following format `Vx` under `src/Api.Server/Models/API/`.

Example:

- For 1.0, stored under `src/Api.Server/Models/API/V1/`
- For 2.0, stored under `src/Api.Server/Models/API/V2/`

## References
