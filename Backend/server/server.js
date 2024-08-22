const fs = require('fs');
const http = require('http');
const express = require('express');
const bodyParser = require('body-parser');
const WebSocket = require('ws');
const cors = require('cors');

const app = express();
app.use(bodyParser.json());
app.use(cors());

const server = http.createServer(app);
const wss = new WebSocket.Server({ server });

const PORT = 8080;

let players = loadPlayers();
let lobbies = [];

// Routers
app.post('/register', (request, response) => {
    const { email, username, password } = request.body;

    if (players.some(user => user.email === email || user.username === username)) {
        return response.status(400).json({ success: false, message: 'Email ou Username já existem!' });
    }

    players.push({ email, username, password });
    savePlayers();

    response.status(201).json({ success: true, message: 'Registro bem sucedido' });
});

app.post('/login', (request, response) => {
    const { username, password } = request.body;
    const player = players.find(player => player.username === username && player.password === password);

    if (player) {
        return response.status(200).json({ success: true, message: 'Login bem-sucedido!' });
    } else {
        return response.status(400).json({ success: false, message: 'Username ou senha incorretos!' });
    }
});

app.post('/createLobby', (request, response) => {
    const { lobbyName, hostUsername } = request.body;

    if (lobbies.some(lobby => lobby.lobbyName === lobbyName)) {
        return response.status(400).json({ success: false, message: 'Já existe um lobby com esse nome!' });
    }

    const newLobby = {
        lobbyName: lobbyName,
        hostUsername: hostUsername,
        players: [hostUsername],
        maxPlayers: 10
    };

    lobbies.push(newLobby);

    return response.status(201).json({ success: true, message: 'Lobby criado com sucesso!' });
});

app.post('/joinLobby', (request, response) => {

    const { lobbyName, username } = request.body;
    const lobby = lobbies.find(lobby => lobby.lobbyName === lobbyName);

    console.log(username);

    if (!lobby) {
        return response.status(400).json({ success: false, message: 'Lobby não encontrado!' });
    }

    if (lobby.players.length >= lobby.maxPlayers) {
        return response.status(400).json({ success: false, message: 'Lobby está cheio!' });
    }

    if (lobby.players.includes(username)) {
        return response.status(400).json({ success: false, message: 'Você já está no lobby!' });
    }

    lobby.players.push(username);

    return response.status(200).json({ success: true, message: 'Você entrou no lobby com sucesso!' })
});

// WebSocket
wss.on('connection', (ws) => {
    ws.on('message', (message) => {
        const data = JSON.parse(message);

        if (data.type === 'login') {
            ws.username = data.username;
        }

        if (data.type === 'joinLobby') {
            ws.lobbyName = data.lobbyName;
        }

        if (data.type === 'lobbyMessage') {
            broadcastToLobby(data.lobbyName, data.message, data.username, ws);
        }

        if (data.type === 'startGame') {
            console.log('startGame');
            const message = { type: 'startGame' }
            startGame(data.lobbyName, JSON.stringify(message));
        }

        if (data.type === 'updatePosition') {

            if (!ws.username) {
                ws.username = data.username;
            }

            if (!ws.lobbyName) {
                ws.lobbyName = data.lobbyName;
            }

            updatePosition(data);
        }
    });
});

server.listen(PORT, () => {
    console.log(`Servidor rodando na porta ${PORT}`);
});

// functions
function loadPlayers() {
    try {
        const data = fs.readFileSync('users.json');
        return JSON.parse(data);
    } catch (error) {
        return [];
    }
}

function savePlayers() {
    fs.writeFileSync('users.json', JSON.stringify(players, null, 2));
}

function broadcastToLobby(lobbyName, message, username, senderSocket) {
    wss.clients.forEach(client => {
        if (client !== senderSocket && client.readyState === WebSocket.OPEN) {
            const data = JSON.stringify({ type: 'lobbyMessage', lobbyName, message, username });
            console.log(`Mensagem Repassada! ${data}`);
            client.send(data);
        }
    });
}

function startGame(lobbyName, message) {

    console.log(`${lobbyName} - ${message}`);

    const lobby = lobbies.find(lobby => lobby.lobbyName == lobbyName);

    if (lobby) {

        console.log('Lobby válido');

        lobby.players.forEach(playerUsername => {

            console.log(playerUsername);

            wss.clients.forEach(client => {

                if (client.username === playerUsername && client.readyState === WebSocket.OPEN) {
                    console.log(`Mensagem enviada para: ${playerUsername}`);
                    client.send(message);
                }
            });
        });
    }
}

function updatePosition(data) {
    console.log(data);

    const lobby = lobbies.find(lobby => lobby.lobbyName == data.lobbyName);

    if (lobby) {
        console.log('------Update Match------');
        lobby.players.forEach(playerUsername => {

            console.log(`Tentando contactar ${playerUsername}`);

            wss.clients.forEach(client => {

                console.log(`Cliente username: ${client.username}`);

                if (client.username === playerUsername && client.readyState === WebSocket.OPEN) {
                    console.log('reenviando ao cliente...');
                    client.send(JSON.stringify(data));
                }
            });
        })
    }
}
