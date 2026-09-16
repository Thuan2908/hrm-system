# Admin Advanced Search Specification

## Target
Trang `/admin/users`.

## Basic Search
Keyword tìm theo:
- Full Name
- Username

## Filters
- Department
- Role
- Account Status: Active / Locked / Inactive
- Optional: Created Date range
- Optional: Last Login range

## Sorting
- CreatedAt ascending/descending
- LastLogin ascending/descending
- Username ascending/descending

## Pagination
- page
- pageSize
- total
- totalPages

## Account Lifecycle
Admin được:
- Create
- View
- Edit metadata được phép
- Reset Password
- Lock
- Unlock
- Deactivate
- Assign Role
- Revoke Role
- Assign Permission
- Revoke Permission

Không dùng hard delete.

## Audit
Mỗi hành động quản trị account phải lưu:
- Actor
- TargetUser
- Action
- Before/After snapshot phù hợp
- Timestamp
