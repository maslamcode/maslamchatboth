const {
    makeWASocket,
    useMultiFileAuthState,
    fetchLatestBaileysVersion,
    DisconnectReason,
    jidNormalizedUser
} = require("@whiskeysockets/baileys");
const pino = require("pino");
const { spawn } = require("child_process");
const fs = require("fs");
const qrcode = require("qrcode-terminal");
const path = require("path");
const QRCode = require("qrcode");

// ==== Express API ====
const express = require("express");
const multer = require("multer");
const API_KEY = "TCH0qIeozGfEkHGOSZuaYJaI3GKjylsnjnwiFMRPmltSsPRbhpyBatvzhYeHco9NnZXSxp628cAZrx5EkInTUqOb7LXBNkECgZFtJDnt07mVyarrAGwGH4W37cKzlSi3";

let waSocket = null; // general holder

let lastQR = null;
let connectionStatus = "idle";


function checkApiKey(req, res, next) {
    const key = req.headers['x-api-key'];

    if (!key || key !== API_KEY) {
        return res.status(401).json({ error: 'Unauthorized' });
    }

    next();
}


const app = express();
const port = 90;

const uploadFolder = path.join(__dirname, "DataFiles");
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

app.get("/test", (req, res) => {
    res.json({ ok: true });
});

app.get("/files", checkApiKey, (req, res) => {
    fs.readdir(uploadFolder, (err, files) => {
        if (err) return res.status(500).json({ error: err.message });
        res.json(files);
    });
});

app.post("/upload", checkApiKey, upload.single("file"), (req, res) => {
    res.json({ message: "✅ File uploaded", file: req.file });
});

app.delete("/files/:name", checkApiKey, (req, res) => {
    const filePath = path.join(uploadFolder, req.params.name);
    fs.unlink(filePath, (err) => {
        if (err) return res.status(500).json({ error: err.message });
        res.json({ message: "🗑️ File deleted" });
    });
});

app.post("/broadcast", checkApiKey, async (req, res) => {
    const { message } = req.body;
    if (!message) {
        return res.status(400).json({ error: "Message required" });
    }
    try {
        await broadcastToAllGroups(waSocket, message);
        res.json({ message: "======= Broadcast sent" });
    } catch (err) {
        res.status(500).json({ error: err.message });
    }
});

app.post("/broadcast-bulk", checkApiKey, async (req, res) => {
    const { message, groupIds } = req.body;
    const now = new Date().toISOString();

    console.log(`\n🕐 [${now}] Incoming /broadcast-bulk request`);
    console.log("Request body:", JSON.stringify(req.body, null, 2));

    if (!message || !Array.isArray(groupIds) || groupIds.length === 0) {
        console.warn("⚠️ Invalid request: message or groupIds missing");
        return res.status(400).json({ error: "Message and groupIds required" });
    }

    try {
        console.log(`📢 Starting broadcast to ${groupIds.length} groups...`);
        await broadcastToGroups(waSocket, message, groupIds);
        console.log(`✅ Broadcast completed to ${groupIds.length} groups at ${now}`);
        res.json({ message: `======= Broadcast sent to ${groupIds.length} groups` });
    } catch (err) {
        console.error("❌ Broadcast error:", err);
        res.status(500).json({ error: err.message });
    }
});

app.post("/broadcast-personal", checkApiKey, async (req, res) => {
    const { message, phoneNumber } = req.body;
    const now = new Date().toISOString();

    console.log(`\n🕐 [${now}] Incoming /broadcast-personal request`);
    console.log("Request body:", JSON.stringify(req.body, null, 2));

    if (!message || !phoneNumber) {
        console.warn("⚠️ Invalid request: message or phoneNumber missing");
        return res.status(400).json({ error: "Message and phoneNumber required" });
    }

    try {
        console.log(`📢 Starting personal broadcast to ${phoneNumber}`);
        await broadcastToPersonals(waSocket, message, [phoneNumber]);
        console.log(`✅ Personal broadcast completed to ${phoneNumber} at ${now}`);
        res.json({ message: `======= Broadcast sent to ${phoneNumber}` });
    } catch (err) {
        console.error("❌ Personal broadcast error:", err);
        res.status(500).json({ error: err.message });
    }
});

//API WA SETUP

app.get("/wa-status", checkApiKey, (req, res) => {
    res.json({
        status: connectionStatus,
        hasQR: lastQR != null,
        isConnected: connectionStatus === "open",
        time: new Date()
    });
});

app.get("/wa-qr", checkApiKey, async (req, res) => {
    if (!lastQR) {
        return res.status(404).json({ error: "QR not available" });
    }

    try {
        const imgBase64 = await QRCode.toDataURL(lastQR);

        res.json({
            qr: lastQR,        
            img_src: imgBase64       
        });

    } catch (err) {
        res.status(500).json({ error: err.message });
    }

});

app.post("/wa-rescan", checkApiKey, async (req, res) => {
    try {
        if (!waSocket) {
            return res.status(400).json({ error: "WA socket not initialized" });
        }
        
        waSocket.ws.close();
        waSocket = null;

        await connectToWhatsApp();

        res.json({ message: "🔁 QR regenerating..." });
    } catch (err) {
        res.status(500).json({ error: err.message });
    }
});

app.post("/wa-restart", checkApiKey, async (req, res) => {
    const ok = await restartWhatsApp();
    if (ok) {
        res.json({ message: "🔄 WhatsApp restarted" });
    } else {
        res.status(500).json({ error: "Failed to restart WhatsApp" });
    }
});




//END API WA SETUP




app.listen(port, () => {
    console.log(`📡 File API running at http://localhost:${port}`);
});

function delay(ms) {
    return new Promise((resolve) => setTimeout(resolve, ms));
}

async function safeSend(socket, jid, content, options = {}) {
    try {
        return await socket.sendMessage(jid, content, options);
    } catch (err) {
        if (err?.message?.includes("No sessions")) {
            console.warn("⚠️ No sessions, nunggu WA kirim senderKey...");
            await delay(8000);
            console.log("🔄 Retry setelah delay 8 detik...");
            return await socket.sendMessage(jid, content, options);
        }
        throw err;
    }
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

    waSocket = socket;

    socket.ev.on("creds.update", saveCreds);

    socket.ev.on("connection.update", async ({ connection, lastDisconnect, qr }) => {
        const now = new Date();

        if (qr) {
            lastQR = qr;

            console.log("🔳 QR code generated. Silakan scan:");
            qrcode.generate(qr, { small: true });
        }

        connectionStatus = connection || "unknown";

        if (connection === "connecting") {
            console.log(now + " 📡 WhatsApp Connecting...");
        } else if (connection === "open") {
            console.log(now + " ✅ WhatsApp Connected.");

            if (socket.user) {
                console.log("My WhatsApp JID:", socket.user.id);
                console.log("My number:", socket.user.id.split(':')[0]);

                if (socket.user.lid) {
                    console.log("📛 My WhatsApp LID:", `@${socket.user.lid}`);
                }

                const creds = await socket.authState.creds;
                if (creds && creds.me && creds.me.lid) {
                    console.log("📛 My WhatsApp LID (from creds):", `@${creds.me.lid}`);
                }
            }


            try {
                const groups = await socket.groupFetchAllParticipating();

                const simplifiedGroups = [];

                for (const id in groups) {
                    const metadata = groups[id];
                    simplifiedGroups.push({
                        group_id: id,
                        group_name: metadata.subject
                    });
                }

                const response = await bulkInsertGroupsToCSharp(simplifiedGroups);
                console.log("📥 Groups bulk insert response:", response);

                for (const id in groups) {
                    const metadata = groups[id];
                    console.log("📌 Prewarming session grup:", metadata.subject);
                    for (const participant of metadata.participants) {
                        await socket.presenceSubscribe(participant.id);
                        await delay(200);
                    }
                }
            } catch (e) {
                console.error("⚠️ Gagal prewarm session grup:", e);
            }

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

                const waitSeconds = Math.floor(Math.random() * 20) + 10; // 10–30 detik
                const waitMs = waitSeconds * 1000;

                console.log(`⏱️ Menunggu ${waitMs} menit sebelum reconnect...`);
                await delay(waitMs);

                connectToWhatsApp();
            } else {
                console.log(now + " 🔄 Reconnecting in 2s...");
                await delay(2000);
                connectToWhatsApp();
            }
        }
    });

    socket.ev.on("messages.upsert", async ({ messages }) => {
        const now = new Date();

        try {
            const message = messages[0];
            if (!message.message) return;

            if (message.message?.senderKeyDistributionMessage) {
                console.log("🔑 Dapat senderKeyDistributionMessage dari:", message.key.remoteJid);
                console.log("📄 Key payload:", message.message.senderKeyDistributionMessage);
            }

            const pesan = getPesan(message);

            const phone = message.key.remoteJid;
            const fromMe = message.key.fromMe;
            const chatId = jidNormalizedUser(phone);

            if (!fromMe && phone.endsWith("@g.us")) {

                const tag = `@${globChatbot.chatbotNumber.nomor?.replace(/[^0-9]/g, '') || ''}`;
                const tag2 = `@${globChatbot.chatbotNumber.id || ''}`;

                //const tag = "@6282260091545"; // ← ganti sesuai nomor kamu, siven
                //const tag2 = "@8603490619632";

                console.log(`tag###################`, tag);
                console.log(`tag2##################`, tag2);

                console.log(now + " 📥 Pesan masuk dari grup:", pesan);

                //const keywords = [
                //    "sami",
                //    "perkenalkan",
                //    "kenalkan",
                //    "kenalin",
                //    "assalamu'alaykum",
                //    "assalamualaikum",
                //    "assalamu'alaikum",
                //    "bertanya",
                //    "tanya",
                //    "nanya",
                //    "maslam",
                //];

                const keywords = globChatbot.chatbotTaskLists
                    .map(t => t.task_list)
                    .filter(Boolean)
                    .flatMap(t =>
                        t.split(",")
                            .map(k => k.trim().toLowerCase())
                            .filter(k => k.length > 0)
                    );

                const cleanText = (pesan || "")
                    .normalize("NFKD")
                    .replace(/[^\p{L}\p{N}\s]/gu, "")
                    .toLowerCase();


                const pesanLower = (cleanText || "").toLowerCase();

                const mentionTriggered =
                    (pesan && pesan.includes(tag)) ||
                    (pesan && pesan.includes(tag2));

                const keywordTriggered = keywords.some(word =>
                    pesanLower.split(/\s+/).includes(word.toLowerCase())
                );


                if (mentionTriggered || keywordTriggered) {
                    const cleanPesan = (cleanText || "")
                        .replace(tag, "")
                        .replace(tag2, "")
                        .trim();

                    await handleMessage(socket, phone, chatId, cleanPesan, message);

                }
            }
            else if (!fromMe && !phone.endsWith("@g.us")) {
                // --- CHAT PERSONAL ---
                console.log(now + " 📥 Pesan personal:", pesan);
                await handleMessage(socket, phone, chatId, pesan, message);
            }
            else {
                console.log(now + " 📥 Pesan masuk (lainnya):", pesan);
            }
        } catch (err) {
            console.error(now + " ❌ Error while processing message:", err);
        }
    });

    //Greeting jika ada yang masuk grup ---------------------------------
    socket.ev.on("group-participants.update", async (update) => {
        try {
            if (update.action === "add") {
                console.log(" 📥 Ada member baru");
                const metadata = await socket.groupMetadata(update.id);
                for (let participant of update.participants) {
                    await socket.sendMessage(update.id, {
                        text: `👋 Assalamu'alaykum Bapak/Ibu @${participant.split("@")[0]}, selamat datang di WA Group *${metadata.subject}*, silahkan untuk memperkenalkan dirinya`,
                        mentions: [participant],
                    });
                }
            }
        } catch (err) {
            console.error("❌ Error welcome message:", err);
        }
    });
}
function getPesan(message) {
    if (!message.message) {
        console.log("⚠️ Tidak ada message.message");
        return "";
    }

    let text = "";

    if (message.message.conversation) {
        text = message.message.conversation;
        console.log("✅ Pesan dari message.conversation:", text);
        return text;
    }

    if (message.message.extendedTextMessage?.text) {
        text = message.message.extendedTextMessage.text;
        console.log("✅ Pesan dari message.extendedTextMessage.text:", text);
        return text;
    }

    if (message.message.imageMessage?.caption) {
        text = message.message.imageMessage.caption;
        console.log("✅ Pesan dari message.imageMessage.caption:", text);
        return text;
    }

    if (message.message.videoMessage?.caption) {
        text = message.message.videoMessage.caption;
        console.log("✅ Pesan dari message.videoMessage.caption:", text);
        return text;
    }

    if (message.message.ephemeralMessage?.message?.conversation) {
        text = message.message.ephemeralMessage.message.conversation;
        console.log("✅ Pesan dari ephemeralMessage.message.conversation:", text);
        return text;
    }

    if (message.message.ephemeralMessage?.message?.extendedTextMessage?.text) {
        text = message.message.ephemeralMessage.message.extendedTextMessage.text;
        console.log("✅ Pesan dari ephemeralMessage.message.extendedTextMessage.text:", text);
        return text;
    }

    console.log("⚠️ Tidak menemukan teks di struktur message:", JSON.stringify(message, null, 2));
    return "";
}

const processedMessages = new Set();

async function handleMessage(socket, phone, chatId, pesan, message) {
    const now = new Date();

    const messageId = message?.key?.id || `${phone}-${now.getTime()}`;

    if (processedMessages.has(messageId)) {
        console.log(now + " ⚠️ Duplicate message detected, skipped:", messageId);
        return;
    }
    processedMessages.add(messageId);

    setTimeout(() => processedMessages.delete(messageId), 60 * 1000); // 1 min keep

    if (!pesan || pesan.trim().length === 0) {
        console.log(now + " ⚠️ Pesan kosong, diabaikan.");
        return;
    }

    try {
        //menambahkan mention user -----------------------
        const sender = message.key.participant || message.key.remoteJid;
        const mentionId = sender.endsWith("@s.whatsapp.net") ? sender : chatId;
        const senderId = message.key.participant || message.key.remoteJid;
        const senderName = message.pushName || senderId.split("@")[0];

        pesan = 'Nama penanya : ' + senderName + '  Pertanyaan : ' + pesan;

        console.log(now + " 📤 Mengirim pesan ke C#...");
        let response = await sentToCSharp(pesan);

        if (!response || !response.trim()) {
            response = "⚠️ Tidak ada balasan dari bot.";
        }

        console.log(now + " 📤 Target:", phone, "→", chatId);


        const sendResult = await safeSend(
            socket,
            phone,
            {
                text: response,
                mentions: [mentionId]
            },
            { quoted: message }
        );
        //------------------------------------------------

        console.log(now + " ✅ Pesan terkirim:", response);
        console.log("📬 Send result:", sendResult);
    } catch (err) {
        console.error(now + " ❌ Error while processing message:", err);
        try {
            await safeSend(socket, phone, {
                text: "Maaf, terjadi kesalahan saat memproses pesan.",
            });
        } catch (sendErr) {
            console.error("❌ Gagal mengirim pesan error:", sendErr);
        }
    }
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

async function getChatbotNumberWithCharacter() {
    return new Promise((resolve, reject) => {
        const process = spawn("dotnet", ["GeminiChatBot.dll", "get-number-with-character"]);

        let result = "";

        process.stdout.on("data", (data) => {
            result += data.toString();
        });

        process.stderr.on("data", (data) => {
            console.error("C# stderr:", data.toString());
        });

        process.on("close", (code) => {
            try {
                const match = result.match(/__JSON_START__(.*)__JSON_END__/s);

                if (!match) {
                    console.error("⚠️ No valid JSON output detected from C#");
                    console.error("Raw output:", result);
                    return reject(new Error("Failed to parse C# JSON output"));
                }

                const jsonString = match[1];
                const data = JSON.parse(jsonString);

                resolve(data);
            } catch (err) {
                console.error("❌ Failed to parse JSON:", err.message);
                console.error("Raw result:", result);
                reject(err);
            }
        });
    });
}

async function bulkInsertGroupsToCSharp(groups) {
    return new Promise((resolve, reject) => {
        const jsonString = JSON.stringify(groups);

        const process = spawn("dotnet", [
            "GeminiChatBot.dll",
            "group-bulk-insert",
            jsonString
        ]);

        let result = "";

        process.stdout.on("data", (data) => {
            result += data.toString();
        });

        process.stderr.on("data", (data) => {
            console.error("C# Error:", data.toString());
        });

        process.on("close", (code) => {
            resolve(result.trim());
        });
    });
}

function saveGroupsToFile(groups) {
    const filePath = path.join(__dirname, "groups.json");
    try {
        fs.writeFileSync(filePath, JSON.stringify(groups, null, 2));
        console.log("====== Groups saved to", filePath);
    } catch (err) {
        console.error("xxxxxx Failed to save groups:", err);
    }
}

function loadGroupsFromFile() {
    const filePath = path.join(__dirname, "groups.json");
    if (fs.existsSync(filePath)) {
        return JSON.parse(fs.readFileSync(filePath, "utf-8"));
    }
    return {};
}

async function broadcastToAllGroups(waSocket, messageText) {
    if (!waSocket) {
        console.error("xxxxxx Socket is not connected!");
        return;
    }

    const groups = loadGroupsFromFile();
    const groupIds = Object.keys(groups);

    console.log(" ====== Broadcasting to", groupIds.length, "groups...");

    for (const groupId of groupIds) {
        try {
            await safeSend(waSocket, groupId, { text: messageText });
            console.log("====== Sent to:", groups[groupId].name, `(${groupId})`);
            await delay(1500);
        } catch (err) {
            console.error("xxxxxx Failed to send to", groupId, ":", err.message);
        }
    }
}

async function broadcastToGroups(waSocket, messageText, groupIds) {
    if (!waSocket) {
        console.error("xxxxxx Socket is not connected!");
        return;
    }

    const finalMessage = messageText.replace(/\\n/g, '\n');


    console.log(" ====== Broadcasting to", groupIds.length, "groups...");

    for (const groupId of groupIds) {
        try {
            await safeSend(waSocket, groupId, { text: finalMessage });
            await delay(1500);
        } catch (err) {
            console.error("xxxxxx Failed to send to", groupId, ":", err.message);
        }
    }
}

async function broadcastToPersonals(waSocket, messageText, phoneNumbers) {
    if (!waSocket) {
        console.error("xxxxxx Socket is not connected!");
        return;
    }

    const finalMessage = messageText.replace(/\\n/g, '\n');

    console.log(" ====== Broadcasting to", phoneNumbers.length, "personal numbers...");

    for (const number of phoneNumbers) {
        try {

            const jid = number.endsWith("@s.whatsapp.net") ? number : `${number}@s.whatsapp.net`;

            await safeSend(waSocket, jid, { text: finalMessage });
            console.log(`✅ Sent to personal number: ${jid}`);
            await delay(1500);
        } catch (err) {
            console.error("xxxxxx Failed to send to", number, ":", err.message);
        }
    }

    console.log(`✅ Completed broadcasting to ${phoneNumbers.length} personal numbers`);
}




// 🟢 Start app
(async () => {
    try {
        console.log("🚀 Initializing chatbot globals...");

        const data = await getChatbotNumberWithCharacter();

        if (!globalThis.globChatbot) {
            globalThis.globChatbot = {};
        }

        globChatbot.chatbotNumber = data.number || null;
        globChatbot.chatbotCharacter = data.character || null;
        globChatbot.chatbotNumberTasks = Array.isArray(data.tasks) ? data.tasks : [];
        globChatbot.chatbotTaskLists = Array.isArray(data.taskLists) ? data.taskLists : [];

        console.log("✅ Chatbot globals loaded:", {
            number: globChatbot.chatbotNumber?.nama || "(none)",
            character: globChatbot.chatbotCharacter?.nama || "(none)",
            tasks: globChatbot.chatbotNumberTasks.length,
            taskLists: globChatbot.chatbotTaskLists.length
        });

        await connectToWhatsApp();
    } catch (err) {
        console.error("❌ Fatal error during startup:", err);

        // Retry logic with delay
        const delay = (ms) => new Promise(res => setTimeout(res, ms));
        console.log("🔁 Restarting in 10 seconds...");
        await delay(10000);

        // Try to reconnect cleanly
        await connectToWhatsApp();
    }
})();

//Start API SETUP
async function restartWhatsApp() {
    console.log("🔄 Restarting WhatsApp socket...");

    try {
        if (waSocket) {
            waSocket.ws.close();
            waSocket = null;
        }

        // optionally clear session
        // fs.rmSync("session", { recursive: true, force: true });

        await connectToWhatsApp();
        return true;
    } catch (err) {
        console.error("❌ Failed restart WA:", err);
        return false;
    }
}
