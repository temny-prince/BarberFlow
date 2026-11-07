document.getElementById('btn-login').addEventListener('click', async () => {
    const email = document.getElementById('email').value;
    const password = document.getElementById('password').value;
    const btn = document.getElementById('btn-login');

    if (!email || !password) {
        return showToast('Заполните все поля', true);
    }

    btn.disabled = true;
    btn.innerText = 'Вход...';

    try {
        const data = await ApiClient.login(email, password);

        if (data && data.token) {
            // Сохраняем токен и имя
            localStorage.setItem('jwt_token', data.token);
            localStorage.setItem('admin_name', data.displayName || data.email);

            // Переходим в админку
            window.location.href = 'admin.html';
        } else {
            showToast('Ошибка: сервер не вернул токен', true);
        }
    } catch (e) {
        showToast(e.message || 'Неверный логин или пароль', true);
    } finally {
        btn.disabled = false;
        btn.innerText = 'Войти в систему';
    }
});