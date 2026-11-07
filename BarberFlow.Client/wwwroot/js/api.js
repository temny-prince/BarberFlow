const API_BASE = 'http://localhost:5212/api';

class ApiClient {
    static async request(endpoint, options = {}) {
        const token = localStorage.getItem('jwt_token');

        const headers = {
            'Content-Type': 'application/json',
            ...(token && { 'Authorization': `Bearer ${token}` }),
            ...options.headers
        };

        try {
            const response = await fetch(`${API_BASE}${endpoint}`, { ...options, headers });

            // Если 401 (токен протух) и мы не на странице логина — выкидываем на логин
            if (response.status === 401 && window.location.pathname.includes('admin')) {
                localStorage.removeItem('jwt_token');
                window.location.href = 'login.html';
                return;
            }

            const data = await response.json().catch(() => null);

            if (!response.ok) throw new Error(data?.message || data || `Ошибка: ${response.status}`);
            return data;
        } catch (error) {
            console.error('API Error:', error);
            throw error;
        }
    }

    // Публичные методы
    static getServices() { return this.request('/services'); }
    static getMasters() { return this.request('/masters'); }
    static getSlots(masterId, date) { return this.request(`/masters/${masterId}/slots?date=${date}`); }
    static bookAppointment(data) { return this.request('/appointments', { method: 'POST', body: JSON.stringify(data) }); }

    // Методы авторизации
    static login(email, password) {
        return this.request('/auth/login', { method: 'POST', body: JSON.stringify({ email, password }) });
    }

    // Методы админа
    static getAdminAppointments() { return this.request('/appointments'); }
    static updateStatus(id, status) {
        return this.request(`/appointments/${id}/status`, {
            method: 'PATCH',
            body: JSON.stringify(status)
        });
    }
}

// Хелпер для уведомлений
function showToast(msg, isError = false) {
    let t = document.getElementById('toast');
    if (!t) {
        t = document.createElement('div');
        t.id = 'toast';
        t.className = 'toast';
        document.body.appendChild(t);
    }
    t.innerText = msg;
    t.style.display = 'block';
    t.classList.toggle('error', isError);
    setTimeout(() => t.style.display = 'none', 3000);
}