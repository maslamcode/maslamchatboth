const {
    makeWASocket,
    useMultiFileAuthState,
    fetchLatestBaileysVersion,
    DisconnectReason
} = require("@whiskeysockets/baileys");
const pino = require("pino");
const { spawn } = require("child_process");
const fs = require("fs");
const qrcode = require("qrcode-terminal");
const path = require("path");


// ==== Express API ====
const express = require("express");
const multer = require("multer");

const app = express();
const port = 3000;

const uploadFolder = path.join(__dirname, "DataPDF");
if (!fs.existsSync(uploadFolder)) {
    fs.mkdirSync(uploadFolder);
}

app.use(express.json());
app.use(express.urlencoded({ extended: true }));

const storage = multer.diskStorage({
    destination: (req, file, cb) => cb(null, uploadFolder),
    filename: (req, file, cb) => cb(null, file.originalname)
});
const upload = multer({ storage });

// API: List file
app.get("/files", (req, res) => {
    fs.readdir(uploadFolder, (err, files) => {
        if (err) return res.status(500).json({ error: err.message });
        res.json(files);
    });
});

// API: Upload file
app.post("/upload", upload.single("file"), (req, res) => {
    res.json({ message: "✅ File uploaded", file: req.file });
});

// API: Delete file
app.delete("/files/:name", (req, res) => {
    const filePath = path.join(uploadFolder, req.params.name);
    fs.unlink(filePath, (err) => {
        if (err) return res.status(500).json({ error: err.message });
        res.json({ message: "🗑️ File deleted" });
    });
});

app.listen(port, () => {
    console.log(`📡 File API running at http://localhost:${port}`);
});

function delay(ms) {
    return new Promise((resolve) => setTimeout(resolve, ms));
}

async function connectToWhatsApp() {
    const { version } = await fetchLatestBaileysVersion();
    const { state, saveCreds } = await useMultiFileAuthState("session");

    const socket = makeWASocket({
        version,
        auth: state,
        printQRInTerminal: false, // deprecated
        logger: pino({ level: "debug" }),
        browser: ["Mac OS", "Safari", "16.0"],
    });

    socket.ev.on("creds.update", saveCreds);

    socket.ev.on("connection.update", async ({ connection, lastDisconnect, qr }) => {
        const now = new Date();

        if (qr) {
            console.log("🔳 QR code generated. Silakan scan:");
            qrcode.generate(qr, { small: true });
        }

        if (connection === "connecting") {
            console.log(now + " 📡 WhatsApp Connecting...");
        } else if (connection === "open") {
            console.log(now + " ✅ WhatsApp Connected.");
        } else if (connection === "close") {
            const reason = lastDisconnect?.error?.output?.statusCode;
            console.log(now + " ❌ WhatsApp Closed. Reason:", reason);

            if (reason === DisconnectReason.loggedOut || reason === 405) {
                console.log(now + " 🗑️ Session expired or 405 error. Deleting session...");

                try {
                    fs.rmSync("session", { recursive: true, force: true });
                } catch (err) {
                    console.error("Gagal menghapus session:", err);
                }

                const waitMinutes = Math.floor(Math.random() * 4) + 2; // 2-5 minutes
                const waitMs = waitMinutes * 60 * 1000;

                console.log(`⏱️ Menunggu ${waitMinutes} menit sebelum reconnect...`);
                await delay(waitMs);

                connectToWhatsApp();
            } else {
                console.log(now + " 🔄 Reconnecting in 5s...");
                await delay(5000);
                connectToWhatsApp();
            }
        }
    });

    socket.ev.on("messages.upsert", async ({ messages }) => {
        const now = new Date();

        try {
            const message = messages[0];
            if (!message.message) return;

            const pesan = message.message?.extendedTextMessage?.text || message.message?.conversation;
            const phone = message.key.remoteJid;
            const fromMe = message.key.fromMe;

            if (!fromMe && phone.endsWith("@g.us")) {
                const tag = "@6281360019090"; // ← Ganti sesuai nomormu
                const tag2 = "@8603490619632";

                //Please put phone on the console
                console.log(now + " 📥 Pesan masuk dari grup:", pesan);

                if ((pesan && pesan.includes(tag)) || (pesan && pesan.includes(tag2))) {
                    const cleanPesan = pesan.replace(tag, "").trim();
                    console.log(now + " 📥 Pesan:", cleanPesan);

                    try {
                        console.log(now + " 📤 Mengirim pesan ke C#...");

                        const response = await sentToCSharp(cleanPesan);
                        await socket.sendMessage(phone, { text: response });

                        console.log(now + " 📤 Pesan terkirim:", response);
                    } catch (err) {
                        console.error(now + " ❌ Error while processing message:", err);
                        await socket.sendMessage(phone, { text: "Maaf, terjadi kesalahan saat memproses pesan." });
                    }
                   
                }
            } else {
                console.log(now + " Pesan masuknya : ", pesan);
            }

        } catch (err) {
            console.error(now + " ❌ Error while processing message:", err);
        }
    });
}

async function sentToCSharp(text) {
    return new Promise((resolve, reject) => {
        const process = spawn("dotnet", ["GeminiChatBot.dll", text]);

        let result = "";

        process.stdout.on("data", (data) => {
            result += data.toString();
        });

        process.stderr.on("data", (data) => {
            console.error("C# stderr:", data.toString());
        });

        process.on("close", (code) => {
            if (code === 0) {
                resolve(result.trim());
            } else {
                reject(`C# process exited with code ${code}`);
            }
        });
    });
}

// 🟢 Start app
(async () => {
    try {
        await connectToWhatsApp();
    } catch (err) {
        console.error("❌ Fatal error:", err);
        console.log("🔁 Restarting in 10 seconds...");
        await delay(10000);
        connectToWhatsApp();
    }
})();
