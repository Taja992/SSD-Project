# VMS — Vulnerability Management System

A small app for tracking security bugs ("findings") in software: someone
reports a bug, an analyst sorts it, a developer fixes it.

This is a Secure Software Development exam project. **You are invited to
attack it** — that is how we find out if the security actually works.

---

## What you need

Only **Docker Desktop**. No .NET, no Node, no code editor.

1. Install it from <https://www.docker.com/products/docker-desktop/>
2. Start it, and wait until it says it is running

---

## Start the app (one command)

Open a terminal and paste **one** of these. It makes a folder called
`vms-test` wherever your terminal is, and puts everything inside it.

**Mac / Linux / Git Bash:**

```bash
mkdir -p vms-test && cd vms-test && curl -fsSL https://codeload.github.com/Taja992/SSD-Project/tar.gz/main | tar -xz --strip-components=1 SSD-Project-main/pentest && cd pentest && docker compose up
```

**Windows PowerShell:**

```powershell
mkdir -Force vms-test | Out-Null; cd vms-test; curl.exe -fsSL -o vms.tgz https://codeload.github.com/Taja992/SSD-Project/tar.gz/main; tar -xzf vms.tgz --strip-components=1 SSD-Project-main/pentest; cd pentest; docker compose up
```

The first start takes a few minutes. Leave the terminal open — when the
scrolling slows down, open **<http://localhost:5173>** in your browser.

---

## Log in

Every password is `1234`.

| Username | Role | Allowed to |
|---|---|---|
| `analyst.test` | Analyst | Create, edit and sort any finding |
| `dev.test` | Developer | Read everything, edit **only** findings assigned to them |
| `pm.test` | PM (Project Manager) | Same as Analyst |
| `auditor.test` | Auditor | Read everything, change **nothing** |

The app starts with 12 fake findings, so there is something to look at.

---

## What to try

The rules in the table above are the target. Try to break them:

- Log in as `dev.test` and change a finding that is **not** assigned to you
- Log in as `auditor.test` and change **anything**
- Put HTML or JavaScript in a finding and see if it runs
  (e.g. `<img src=x onerror=alert(1)>`)
- Find a reporter's email address anywhere in the app or its responses
- Get into the app without a valid login

Tip: the browser's **DevTools → Network** tab shows every request the app
sends. You can copy one, change it, and send it again — the buttons in the
page are not the only way in.

**Before you start, read the rules** — what is in scope, and what is already
known so you don't report it: [`pentest/README.md`](pentest/README.md)

---

## Stop the app

Press `Ctrl+C` in the terminal. To delete everything and start fresh next time:

```bash
docker compose down -v
```

(Run it from inside `vms-test/pentest`.) After that you can delete the whole
`vms-test` folder — that is everything.

---

## Something broke?

| Problem | Try |
|---|---|
| `docker: command not found` | Docker Desktop is not installed or not running |
| Page will not load | Wait a minute — the login server starts slowly |
| "Port already in use" | Something else uses port 5173, 8080 or 5148. Close it |
| Login loops or fails | Wait 30 seconds and try again |
