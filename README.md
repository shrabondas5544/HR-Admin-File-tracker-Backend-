# 🗄️ CabinetMap - HR & Admin File Tracker System

Welcome to **CabinetMap**! This is a complete enterprise HR & Admin document tracking system with interactive 3D cabinet wall elevations, drag-and-drop file management, universal search (`Ctrl+K`), archive transfer boxes, and trash bin recovery.

---

## 🌟 What is CabinetMap?

CabinetMap helps HR and Admin teams visually organize physical files, folders, and box files inside office storage cabinets.

### 🚀 Key Features:
- 🏢 **Multi-Wall Elevation View**:
  - **Wall 1**: 6 standard 6-shelf cabinets (Cabinets 1 to 6).
  - **Wall 2**: 2 modular 3-door cabinets (`Cabinet 1 (W2)` & `Cabinet 2 (W2)`) separated by a toilet door.
  - **Wall 3 (Right & Left)**: Extra wall views.
- 📦 **Archive Transfer Box**: Drag & drop files into the top archive box to move items easily between Wall 1 and Wall 2.
- 🖐️ **Drag & Drop Organization**: Drag files into magazines, reorder items on shelves, or move items across cabinets.
- 🔍 **Instant Search (`Ctrl + K`)**: Find any employee name, ID number, department, or file code instantly.
- 🗑️ **Trash Bin & Recovery**: Soft-delete items to the trash modal, filter deleted items, restore them back to their original shelf, or permanently delete them.

---

## 👶 Complete Windows Setup Guide using VS Code Terminal (Step-by-Step for Beginners)

Even if you have **never installed programming tools before**, follow these step-by-step instructions using **Visual Studio Code**!

---

### 📥 Step 1: Install Required Tools

Before starting, download and install these 4 free tools on your Windows computer:

1. **Git (Repository Downloader)**:
   - Go to: **[https://git-scm.com/download/win](https://git-scm.com/download/win)**
   - Click **"64-bit Git for Windows Setup"**.
   - Open the installer and click **"Next"** on every screen until installed, then click **"Finish"**.

2. **Node.js (Runs the Frontend Web Page)**:
   - Go to: **[https://nodejs.org/](https://nodejs.org/)**
   - Click the green button **"LTS (Recommended For Most Users)"**.
   - Open the downloaded `.msi` file, click **"Next"**, check *"I accept the agreement"*, and click **"Install"**.

3. **.NET SDK (Runs the Backend Database & API)**:
   - Go to: **[https://dotnet.microsoft.com/download/dotnet/10.0](https://dotnet.microsoft.com/download/dotnet/10.0)** (or version 9.0/8.0).
   - Under **Windows**, click **SDK x64** to download.
   - Open installer, click **"Install"**, then click **"Close"** when finished.

4. **Visual Studio Code (VS Code Editor)**:
   - Go to: **[https://code.visualstudio.com/](https://code.visualstudio.com/)**
   - Click **"Download for Windows"**.
   - Open the installer, select *"I accept"*, check **"Add to PATH"**, and click **"Install"**.

---

### 📂 Step 2: Create a Main Folder on Your Computer

1. Open your Windows **File Explorer** (yellow folder icon on taskbar).
2. Go to your **Desktop** or **Documents** folder.
3. Right-click in an empty space -> **New** -> **Folder**.
4. Name the new folder: **`HR-Admin-File-tracker`**

---

### 💻 Step 3: Open Your Project Folder in VS Code

1. Open **Visual Studio Code** from your desktop or Windows Start Menu.
2. Click **File** (top-left menu) -> **Open Folder...**
3. Select the **`HR-Admin-File-tracker`** folder you created on your Desktop and click **Select Folder**.
4. If VS Code asks *"Do you trust the authors of the files in this folder?"*, click **"Yes, I trust the authors"**.

---

### 🖥️ Step 4: Open the VS Code Integrated Terminal

1. At the top menu of VS Code, click **Terminal** -> **New Terminal** (or press **`Ctrl + ~`** on your keyboard).
2. A terminal panel will open at the bottom of VS Code!

---

### 📥 Step 5: Clone BOTH Frontend & Backend Repositories

In the VS Code terminal window at the bottom of your screen, copy and run these two commands **one by one**:

1. **Clone Frontend Repository**:
   ```cmd
   git clone https://github.com/shrabondas5544/HR-Admin-File-tracker-frontend.git frontend
   ```
   *(Press **Enter** and wait until it finishes downloading).*

2. **Clone Backend Repository**:
   ```cmd
   git clone https://github.com/shrabondas5544/HR-Admin-File-tracker-Backend-.git backend
   ```
   *(Press **Enter** and wait until it finishes downloading).*

Now you will see two sub-folders inside your VS Code file sidebar: **`frontend`** and **`backend`**!

---

### ⚙️ Step 6: Install Frontend Packages

1. In the VS Code terminal, enter the `frontend` directory:
   ```cmd
   cd frontend
   ```
2. Download all required packages:
   ```cmd
   npm install
   ```
3. Wait 1 to 2 minutes until installation completes.

---

### 🚀 Step 7: Run Backend & Frontend in VS Code

You can run both servers side-by-side using VS Code's split terminal windows!

#### A. Start Backend Server (Database & API):
1. In the VS Code terminal, move from `frontend` to `backend`:
   ```cmd
   cd ..\backend
   ```
2. Start the backend:
   ```cmd
   dotnet run
   ```
3. You will see:
   `Now listening on: http://localhost:5000`

#### B. Start Frontend Web Server:
1. In the VS Code terminal panel (top-right corner of the terminal window), click the **`+`** icon (or split terminal icon) to open a **second terminal tab**.
2. In the new terminal tab, navigate to the `frontend` folder:
   ```cmd
   cd frontend
   ```
3. Start the frontend web server:
   ```cmd
   npm run dev
   ```
4. You will see:
   `- Local: http://localhost:3000`

---

### 🎉 Step 8: Open CabinetMap in Your Browser!

1. Open **Google Chrome** or **Microsoft Edge**.
2. Go to: **[http://localhost:3000](http://localhost:3000)**
3. **Congratulations! 🥳 CabinetMap is now live on your computer!**

---

## 🛠️ VS Code Terminal Commands Cheatsheet

| Action | Terminal 1 (Backend Server) | Terminal 2 (Frontend Server) |
| :--- | :--- | :--- |
| **Navigate to Folder** | `cd backend` | `cd frontend` |
| **Start Command** | `dotnet run` | `npm run dev` |
| **Local URL** | `http://localhost:5000` | `http://localhost:3000` |

---

## 🏗️ Repository Structure

```
HR-Admin-File-tracker/
├── frontend/                     # https://github.com/shrabondas5544/HR-Admin-File-tracker-frontend.git
│   ├── src/
│   │   ├── app/                  # Next.js App Router (page.tsx)
│   │   ├── components/           # Cabinet, Shelf, Drag & Drop, Search & Modal components
│   │   └── lib/                  # TypeScript interfaces and API handlers
│   └── package.json
└── backend/                      # https://github.com/shrabondas5544/HR-Admin-File-tracker-Backend-.git
    ├── Controllers/              # REST API Endpoints (Cabinets, Files, Search, Trash)
    ├── Data/                     # DbContext and DbInitializer (Auto Seed Data)
    ├── Models/                   # C# Data Models (Cabinet, Shelf, Magazine, Folder, File)
    └── CabinetMap.Api.csproj
```

---

## 🤝 Troubleshooting

- **Question**: *Why does frontend show connection error?*
  - **Fix**: Ensure the backend `dotnet run` terminal is running on `http://localhost:5000` before opening `http://localhost:3000`.
- **Question**: *How do I stop the servers in VS Code?*
  - **Fix**: Click on the terminal window in VS Code and press `Ctrl + C` on your keyboard.

---

Made with ❤️ for HR & Admin File Tracker. Happy archiving! 📁✨
