
document.addEventListener('DOMContentLoaded', () => {

    const showRegisterFormButton = document.getElementById('show-register-form');
    const showLoginFormButton = document.getElementById('show-login-form');
    const registerForm = document.getElementById('register-form');
    const loginForm = document.getElementById('login-form');
    const statusElement = document.getElementById('status');

    let ws;

    showRegisterFormButton.addEventListener('click', () => {
        registerForm.style.display = 'block';
        loginForm.style.display = 'none';
    });

    showLoginFormButton.addEventListener('click', () => {
        registerForm.style.display = 'none';
        loginForm.style.display = 'block';
    });

    registerForm.addEventListener('submit', async (event) => {

        event.preventDefault(); // Impede o comportamento padrão de recarregar a página

        const email = document.getElementById('register-email').value;
        const username = document.getElementById('register-username').value;
        const password = document.getElementById('register-password').value;

        const response = await fetch('http://localhost:8080/register', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ email, username, password }),
        });

        const result = await response.json();
        alert(result.message);

    });

    loginForm.addEventListener('submit', async (event) => {
        event.preventDefault();

        const username = document.getElementById('login-username').value;
        const password = document.getElementById('login-password').value;

        const response = await fetch('http://localhost:8080/login', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ username, password }),
        });

        const result = await response.json();
        alert(result.message);

        if (result.success) {

            ws = new WebSocket('ws://localhost:8080');

            ws.onopen = () => {
                statusElement.innerText = 'Connected to Lobby';
                console.log('Conexão Aberta!');
                ws.send(JSON.stringify({ type: 'communication-server', message: 'Olá servidor!', username: username }));
            };

            ws.onmessage = (event) => {
                const data = JSON.parse(event.data);
                console.log('Mensagem recebida:', data);
            };

            ws.onclose = () => {
                statusElement.innerText = 'Disconnected';
                console.log('Conexão fechada');
            };

            ws.onerror = (error) => {
                statusElement.innerText = 'Error';
                console.error('Erro na conexão', error);
            };
        }
    });

});
