# Backend Rules
- NestJS.
- Controller chỉ xử lý HTTP concern.
- Service/Application xử lý use case.
- Repository xử lý persistence.
- DTO phải validate input.
- Mọi endpoint nhạy cảm phải có permission guard.
- Không trả `passwordHash`, token secret hoặc dữ liệu nội bộ nhạy cảm.
