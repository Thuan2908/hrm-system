# API Conventions

Base: `/api/v1`

## Success
```json
{
  "success": true,
  "data": {},
  "meta": {}
}
```

## Error
```json
{
  "success": false,
  "error": {
    "code": "EMPLOYEE_NOT_FOUND",
    "message": "Employee not found",
    "details": []
  }
}
```

## Pagination
`page`, `pageSize`, `sort`, filters theo query params.

## HTTP
- GET: read
- POST: create/action
- PATCH: partial update
- DELETE: chỉ dùng khi thật sự phù hợp; dữ liệu lịch sử ưu tiên status transition
