# Security Rules
- JWT access token + refresh token.
- RBAC theo default deny.
- Password phải hash mạnh.
- Admin route tách riêng về layout/guard.
- Audit các thao tác: login nhạy cảm, khóa/mở tài khoản, đổi quyền, payroll finalize, restore.
- Không log token, password, bank account đầy đủ.
