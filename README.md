# Jules File System API

This is a **.NET 8 Web API** solution built in **Visual Studio 2022**, designed for managing file system operations such as authentication, file/folder manipulation, and more.

## 🔧 Requirements

- Visual Studio 2022
- .NET 8 SDK
- Postman for testing (Swagger opens automatically on run)

---

## ▶️ Getting Started

1. Clone the repository
2. Open the `.sln` file in **Visual Studio 2022**
3. Build the solution
4. Press **F5** to run the project

This will launch a Swagger UI in your default browser where you can interact with all available endpoints.

---

## 🔐 Authentication Workflow

Before accessing protected endpoints, you **must register and log in** via the `AuthController`. Here's how:

### 1. Register

**Endpoint:** `POST /api/Auth/register`

**Body:**
```json
{
  "username": "your_username",
  "password": "your_password"
}
```


### 2. Log In and Get JWT Token

**Endpoint:** `POST /api/Auth/login`

**Request Body:**
```json
{
  "username": "your_username",
  "password": "your_password",
  "role": "user" // or "admin"
}
```

**Response:**
{
  "token": "your_jwt_token"
}

### 3. Authorize in Swagger UI

1. Click the **Authorize** button in the top right of Swagger.
2. Paste the token in the input field using the following format:

**Request Body:**
```
{
  Bearer your_jwt_token
}
```

3. Click **Authorize** and close the dialog.

You are now authenticated and can access protected endpoints.

## 📁 File System API Endpoints

This `FileSystemManagerController` provides endpoints for file and folder management, including uploading, downloading, listing, and deleting files and folders. **JWT authorization is required** for most operations.

---

### 🔒 Important: Folder Path Requirements

- All folder paths **must start with the username**, e.g.:

```
  JulesVerne/my-folder/
```
- **Folders must end with a trailing slash `/`**


Failing to follow this format may result in unauthorized or invalid request errors.

---

### 📤 `POST /api/FileSystem/CreateFile`

Uploads and creates a file in the specified folder.

- **Request Type:** `multipart/form-data`
- **Body:** `FileRequest` (contains `File`, `FolderPath`)
- **Responses:**
	- `200 OK` – File created
	- `400 BadRequest` – Invalid request
	- `403 Forbidden` – Unauthorized

---

### 📁 `POST /api/FileSystem/CreateFolder`

Creates a new folder at the given path.

- **Query Param:** `folderPath`
- **Responses:**
	- `200 OK` – Folder created
	- `400 BadRequest` – Missing path
	- `403 Forbidden` – Unauthorized

---

### 📥 `GET /api/FileSystem/DownloadFile`

Reads a file from a specified path.

- **Query Param:** `path`
- **Responses:**
	- `200 OK` – File content
	- `400 BadRequest` – Missing path
	- `403 Forbidden` – Unauthorized

---

### 🗑️ `DELETE /api/FileSystem/Delete`

Deletes a file or folder.

- **Query Param:** `path`
- **Responses:**
	- `200 OK` – Item deleted
	- `400 BadRequest` – Missing path
	- `403 Forbidden` – Unauthorized

---

### 📄 `GET /api/FileSystem/ListItems`

Lists files and folders in a given directory.

- **Query Param:** `folderPath`
- **Responses:**
	- `200 OK` – List of files/folders
	- `400 BadRequest` – Missing path

---

### 🔍 `GET /api/FileSystem/FindInFiles`

Searches for a string in the content of all files in a folder.

- **Query Params:**
- `folder` – Folder to search in
- `searchstring` – Text to search for
- **Responses:**
	- `200 OK` – List of matching items
	- `400 BadRequest` – Missing inputs

---

### ✅ Tips for Using the API

- Always ensure your JWT token is included in requests for protected routes.
- Use Swagger's **Authorize** button to add your token (`Bearer your_jwt_token`) before testing endpoints.
- Follow folder path conventions strictly to avoid unexpected errors.