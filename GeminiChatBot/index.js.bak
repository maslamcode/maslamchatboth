const { makeWASocket, useMultiFileAuthState } = require("@whiskeysockets/baileys")
const pino = require("pino")
const { spawn } = require('child_process');

async function connectToWhatsApp() {
    const authState = await useMultiFileAuthState("session")
    const socket = makeWASocket({
        printQRInTerminal: true,
        browser: ["windows", "chrome", "10"],
        auth: authState.state,
        logger: pino({ level: "silent" })
    })


    socket.ev.on('creds.update', authState.saveCreds)
    socket.ev.on('connection.update', ({ connection, qr }) => {
        if (connection === 'open') {
            console.log('Whatsapp  Active..')
        } else if (connection === 'close') {
            console.log('Whatsapp Closed..')
            connectToWhatsApp()
        } else if (connection === 'connecting') {
            console.log('Whatsapp Connecting')
        }
        if (qr) {

            console.log('qr', qr)
        }
    })
    socket.ev.on('messages.upsert', async ({ messages }) => {
		let nows = new Date();
        try {
			console.log('raw message',messages);
            const message = messages[0];
			const pesan = message.message.extendedTextMessage.text;
            const phone = message.key.remoteJid;
			const groupMetadata = await socket.groupMetadata(phone);
            console.log(nows+' ' + "Group Name:", groupMetadata.subject);
            const fromMe = message.key.fromMe;
            console.log(nows+' ['+groupMetadata.subject+']' + 'From me:', fromMe);
            console.log(nows+' ['+groupMetadata.subject+']' + 'phone me:', phone);

            // Check if the message is from a group and not from the bot itself
            if (!fromMe && phone.endsWith('@g.us')) {
                // Define a tag to search for, such as "@bot" or "#tag"
                const tag = '@6281235022976';  // Change this to your desired tag/mention

                // Check if the message contains the tag
                if (pesan && pesan.includes(tag)) {
                    try {
                        //const pesans = pesan.split('@')[0].trim();
						const pesans = pesan.replace("@6281235022976", ""); // Remove @6281235022976 followed by a space
						console.log(nows+' ['+groupMetadata.subject+']' + 'pesan WA:', pesans);
                        // Send the message content to the C# backend and wait for the response
                        const response = await sentToCSharp(pesans);
                        const text = response;  // Handle the response data
                        console.log(nows+' ['+groupMetadata.subject+']' + 'response AI:', text);

                        // Send the response back to the group chat
                         const sentMsg =await socket.sendMessage(phone, { text: text });
						 //console.log("Message sent successfully:", sentMsg);

                    } catch (error) {
                        console.error(nows+' ['+groupMetadata.subject+']' + 'Error while processing C# response:', error);
                    }
                }
            }
        } catch (error) {
            console.error(nows+' ' + 'Error while processing C# response:', error);
        }
    });


}
connectToWhatsApp()

async function sentToCSharp(question) {
    return new Promise((resolve, reject) => {
        const csharpProcess = spawn('dotnet', ['GeminiChatBot.dll', "TeS", question]);

        let responseData = '';

        // Collect data from the C# process stdout
        csharpProcess.stdout.on('data', (data) => {
            responseData += data.toString();
        });

        // Handle errors from the C# process
        csharpProcess.stderr.on('data', (data) => {
            console.error('C# Error:', data.toString());
        });

        // When the C# process closes, resolve or reject the promise
        csharpProcess.on('close', (code) => {
            if (code === 0) {
                resolve(responseData);  // Resolve with the accumulated response
            } else {
                reject(`C# process exited with code ${code}`);
            }
        });
    });

}