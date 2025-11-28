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

let waSocket = null;
let waConnectingPromise = null;
let lastQR = null;
let lastQRTimestamp = 0;
const QR_DEBOUNCE_MS = 5000;

const sessionPath = path.resolve(__dirname, "session");

let connectionStatus = "idle";

const port = 90;

const fullDomain = `http://172.104.163.223:${port}`;


function checkApiKey(req, res, next) {
    const key = req.headers['x-api-key'];

    if (!key || key !== API_KEY) {
        return res.status(401).json({ error: 'Unauthorized' });
    }

    next();
}


const app = express();

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
    if (connectionStatus === "open") {
        return res.json({
            message: "✅ WhatsApp is already connected. Please rescan if you need a new QR."
        });
    }

    if (!lastQR) {
        return res.status(404).json({ error: "QR not available, please rescan." });
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

        const sessionPath = path.resolve(__dirname, "session");

        if (fs.existsSync(sessionPath)) {
            try {
                fs.rmSync(sessionPath, { recursive: true, force: true });
                console.log("✅ Session folder removed");
            } catch (err) {
                console.error("❌ Failed to remove session folder:", err);
                return res.status(500).json({ error: "Failed to delete session folder" });
            }
        } else {
            console.log("⚠️ Session folder does not exist");
        }

        await connectToWhatsApp(true);

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
    console.log(`📡 File API running at ${fullDomain}`);
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

async function connectToWhatsApp(forceRestart = false) {
    if (waConnectingPromise && !forceRestart) {
        console.log("⚠️ Already connecting, waiting for existing connection...");
        return waConnectingPromise;
    }

    if (forceRestart && waConnectingPromise) {
        console.log("🔁 Force restarting current WhatsApp connection...");
        try {
            waSocket?.ws?.close();
        } catch { }
        waSocket = null;
        waConnectingPromise = null;
        lastQR = null;
        lastQRTimestamp = 0;
        await delay(1000);
    }

    waConnectingPromise = (async () => {
        try {

            const { version } = await fetchLatestBaileysVersion();
            const { state, saveCreds } = await useMultiFileAuthState("session");

            const socket = makeWASocket({
                version,
                auth: state,
                printQRInTerminal: false, // deprecated
                logger: pino({ level: "debug" }),
                browser: ["Mac OS", "Safari", "16.0"],
                connectTimeoutMs: 60000,
                qrTimeout: 60000, 
                defaultQueryTimeoutMs: 60000, 
                keepAliveIntervalMs: 25000, 
            });

            waSocket = socket;

            socket.ev.on("creds.update", saveCreds);

            socket.ev.on("connection.update", async ({ connection, lastDisconnect, qr }) => {
                const now = new Date();

                if (qr) {
                    const currentTime = Date.now();
                    const timeSinceLastQR = currentTime - lastQRTimestamp;

                    // Only generate new QR if enough time has passed OR if its the first QR
                    if (lastQRTimestamp === 0 || timeSinceLastQR >= QR_DEBOUNCE_MS) {
                        lastQR = qr;
                        lastQRTimestamp = currentTime;

                        console.log("\n🔳 QR Code Generated - Please scan within 60 seconds:");
                        console.log("⏰ Time:", now.toLocaleTimeString());
                        qrcode.generate(qr, { small: true });
                        console.log("\n");
                    } else {
                        console.log(`⏳ QR update ignored (debounced - ${Math.round(timeSinceLastQR / 1000)}s since last)`);
                    }
                }

                connectionStatus = connection || "unknown";

                if (connection === "connecting") {
                    console.log(now + " 📡 WhatsApp Connecting...");
                }
                else if (connection === "open") {
                    console.log(now.toLocaleTimeString() + " ✅ WhatsApp Connected Successfully!");

                    // Reset QR tracking on successful connection
                    lastQR = null;
                    lastQRTimestamp = 0;

                    if (socket.user) {
                        const waId = socket.user.id;
                        const phone = waId.split(":")[0].split("@")[0];

                        console.log("My WhatsApp JID:", waId);
                        console.log("My number:", phone);

                        if (socket.user.lid) {
                            console.log("📛 My WhatsApp LID:", `@${socket.user.lid}`);
                        }

                        const creds = await socket.authState.creds;
                        if (creds && creds.me && creds.me.lid) {
                            const waLidNumber = creds.me.lid.replace("@lid", "").split(":")[0];
                            console.log("📛 My WhatsApp LID (from creds):", `@${creds.me.lid}`);

                            updateWhatsappConnected(phone, waLidNumber);
                        }
                    }


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

                }
                else if (connection === "close") {
                    console.log("❌ WhatsApp Connection Closed");

                    // Reset QR tracking
                    lastQR = null;
                    lastQRTimestamp = 0;

                    const reason = lastDisconnect?.error?.output?.statusCode;
                    console.log("📋 Disconnect reason code:", reason);

                    if (reason === DisconnectReason.loggedOut || reason === 405) {
                        console.log("🔐 Logged out - clearing session...");

                        try {
                            fs.rmSync("session", { recursive: true, force: true });
                        } catch { }
                        waSocket = null;
                        waConnectingPromise = null;
                        await delay(2000);
                        return connectToWhatsApp(true); // force restart with new session
                    }

                    // Automatic reconnect for normal close
                    console.log("🔄 Reconnecting in 2 seconds...");
                    await delay(2000);
                    waSocket = null;
                    waConnectingPromise = null;
                    return connectToWhatsApp(false); // normal restart, no QR
                }
            });

            socket.ev.on("messages.upsert", async ({ messages }) => {
                const now = new Date();

                console.log(`!--------------------------------messages:-------------`, messages)

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

                        //console.log(`globChatbot###################`, globChatbot);
                        console.log(`globChatbot.chatbotTaskLists###################`, globChatbot.chatbotTaskLists.length);

                        const keywords = globChatbot.chatbotTaskLists
                            .map(t => t.task_list)
                            .filter(Boolean)
                            .flatMap(t =>
                                t.split(",")
                                    .map(k => k.trim().toLowerCase())
                                    .filter(k => k.length > 0)
                            );

                        //console.log(`keywords.keywords###################`, globChatbot.chatbotTaskLists);

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

            return socket;

        } catch (error) {
            console.error("❌ Fatal error in connectToWhatsApp:", error);
            waConnectingPromise = null;
            throw error;
        } finally {
            // Only clear promise if connection succeeded or failed completely
        }
    })();

    return waConnectingPromise;

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

            await connectToWhatsApp(false);
        } catch (err) {
            console.error("❌ Fatal error during startup:", err);

            // Retry logic with delay
            const delay = (ms) => new Promise(res => setTimeout(res, ms));
            console.log("🔁 Restarting in 10 seconds...");
            await delay(10000);

            // Try to reconnect cleanly
            await connectToWhatsApp(false);
        }
    })();

    //Start API SETUP
    async function restartWhatsApp() {
        console.log("🔄 Full app restart requested...");

        try {
            if (waSocket) {
                try { waSocket.end?.(); } catch { }
                try { waSocket.ws?.close(); } catch { }
            }

            // Give it a moment to close
            await new Promise(res => setTimeout(res, 500));

            console.log("🛑 Exiting process...");
            process.exit(0);

        } catch (err) {
            console.error("❌ Error while restarting:", err);
            process.exit(1);
        }
    }


    function updateWhatsappConnected(phoneNumber, whatsappId) {
        return new Promise((resolve, reject) => {

            const process = spawn("dotnet", [
                "GeminiChatBot.dll",
                "whatsapp-connected",
                phoneNumber,
                whatsappId
            ]);

            let output = "";
            let errorOutput = "";

            process.stdout.on("data", (data) => {
                output += data.toString();
            });

            process.stderr.on("data", (data) => {
                errorOutput += data.toString();
            });

            process.on("close", (code) => {
                if (code === 0) {
                    console.log("C# whatsapp-connected success:", output.trim());
                    resolve(output.trim());
                } else {
                    console.error("C# whatsapp-connected error:", errorOutput);
                    reject(new Error(errorOutput));
                }
            });
        });
    }

    //UI SETUP
    let passwordSetup = "maslam-chatbot";

    app.get("/wa-status-proxy", async (req, res) => {
        try {
            const response = await fetch(`${fullDomain}/wa-status`, {
                headers: { "x-api-key": API_KEY }
            });
            const data = await response.json();
            res.json(data);
        } catch (err) {
            res.status(500).json({ error: err.message });
        }
    });

    app.post("/wa-qr-proxy", async (req, res) => {
        const { password } = req.body;
        if (password !== passwordSetup) return res.status(401).json({ error: "Wrong password" });

        try {
            const response = await fetch(`${fullDomain}/wa-qr`, {
                headers: { "x-api-key": API_KEY }
            });
            const data = await response.json();
            res.json(data);
        } catch (err) {
            res.status(500).json({ error: err.message });
        }
    });

    app.post("/wa-rescan-proxy", async (req, res) => {
        const { password } = req.body;
        if (password !== passwordSetup) return res.status(401).json({ error: "Wrong password" });

        try {
            const response = await fetch(`${fullDomain}/wa-rescan`, {
                method: "POST",
                headers: { "x-api-key": API_KEY }
            });
            const data = await response.json();
            res.json(data);
        } catch (err) {
            res.status(500).json({ error: err.message });
        }
    });

    app.post("/wa-restart-proxy", async (req, res) => {
        const { password } = req.body;
        if (password !== passwordSetup) return res.status(401).json({ error: "Wrong password" });

        try {
            const response = await fetch(`${fullDomain}/wa-restart`, {
                method: "POST",
                headers: { "x-api-key": API_KEY }
            });
            const data = await response.json();
            res.json(data);
        } catch (err) {
            res.status(500).json({ error: err.message });
        }
    });

    // --- UI page ---
    app.get("/wa-setup", (req, res) => {
        res.send(`
<!DOCTYPE html>
<html lang="en">
<head>
<meta charset="UTF-8">
<title>WhatsApp Setup</title>
<script src="https://code.jquery.com/jquery-3.7.0.min.js"></script>
<style>
    body { font-family: Arial, sans-serif; padding: 20px; }
    button { margin: 5px 0; padding: 10px 15px; }
    #qr-modal { display: none; position: fixed; top:0; left:0; width:100%; height:100%; background: rgba(0,0,0,0.5); justify-content:center; align-items:center; }
    #qr-modal-content { background: #fff; padding:20px; border-radius:5px; text-align:center; max-width:350px; }
    #qr-modal img { max-width: 300px; margin:10px 0; }
</style>
</head>
<body>

<h1>WhatsApp API Setup</h1>

<div id="status">
    <strong>Status:</strong> <span id="connection-status">Loading...</span>
</div>

<button id="btn-show-qr">Show QR Code</button><br>
<button id="btn-rescan">Rescan QR</button><br>
<button id="btn-restart">Restart WhatsApp</button>

<div id="qr-modal">
    <div id="qr-modal-content">
        <h3>Scan QR Code</h3>
        <div id="qr-msg" style="margin-bottom: 10px; color: green;"></div>
        <img id="qr-img" src="" alt="QR Code"/>
        <br>
        <button id="btn-close-qr">Close</button>
    </div>
</div>

<script>
let qrInterval = null;

async function fetchStatus() {
    $.get("/wa-status-proxy", function(res){
        $("#connection-status").text(res.status + (res.isConnected ? " ✅" : " ❌"));
        if(res.isConnected) stopQRInterval();
    }).fail(function(err){
        console.error(err);
        $("#connection-status").text("Error fetching status");
    });
}

async function fetchQR(password) {
    return $.ajax({
        url: "/wa-qr-proxy",
        method: "POST",
        contentType: "application/json",
        data: JSON.stringify({ password }),
        success: function(res){
            if(res.img_src) {
                $("#qr-img").show().attr("src", res.img_src);
                $("#qr-msg").text("");
            } else if(res.message) {
                $("#qr-img").hide();
                $("#qr-msg").text(res.message);
            }
        },
        error: function(err){
            console.error(err);
            $("#qr-img").hide();
            $("#qr-msg").text(err.responseJSON?.error || "Failed to fetch QR");
        }
    });
}

function startQRInterval(password) {
    fetchQR(password);
    if(qrInterval) clearInterval(qrInterval);
    qrInterval = setInterval(() => fetchQR(password), 5000);
}

function stopQRInterval() {
    if(qrInterval) clearInterval(qrInterval);
}

// --- Button actions ---
$("#btn-show-qr").click(async () => {
    const password = prompt("Enter setup password:");
    if(!password) return;
    $("#qr-modal").css("display", "flex");
    startQRInterval(password);
});

$("#btn-close-qr").click(() => {
    $("#qr-modal").hide();
    stopQRInterval();
});

$("#btn-rescan").click(async () => {
    const password = prompt("Enter setup password:");
    if(!password) return;
    $.ajax({
        url: "/wa-rescan-proxy",
        method: "POST",
        contentType: "application/json",
        data: JSON.stringify({ password }),
        success: function(res){
            alert(res.message);
            fetchStatus();
        },
        error: function(err){
            alert(err.responseJSON?.error || "Failed to rescan QR");
        }
    });
});

$("#btn-restart").click(async () => {
    const password = prompt("Enter setup password:");
    if(!password) return;
    $.ajax({
        url: "/wa-restart-proxy",
        method: "POST",
        contentType: "application/json",
        data: JSON.stringify({ password }),
        success: function(res){
            alert(res.message);
            fetchStatus();
        },
        error: function(err){
            alert(err.responseJSON?.error || "Failed to restart WA");
        }
    });
});

// auto-refresh status every 10s
setInterval(fetchStatus, 10000);
</script>

</body>
</html>
    `);
    });
