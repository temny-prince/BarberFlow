// ==========================================
// ИНИЦИАЛИЗАЦИЯ И АВТОРИЗАЦИЯ
// ==========================================
document.addEventListener('DOMContentLoaded', () => {
    const token = localStorage.getItem('jwt_token');
    if (!token) {
        window.location.href = 'login.html';
        return;
    }

    document.getElementById('adminName').innerText = localStorage.getItem('admin_name') || 'Админ';

    // Загружаем первую вкладку по умолчанию
    loadAppointments();

    // Обработчик выхода
    document.getElementById('btn-logout').addEventListener('click', () => {
        localStorage.clear();
        window.location.href = 'login.html';
    });
});

// ==========================================
// ПЕРЕКЛЮЧЕНИЕ ВКЛАДОК
// ==========================================
window.switchTab = (tabName, btnElement) => {
    // Снимаем активность со всех кнопок и контента
    document.querySelectorAll('.tab').forEach(t => t.classList.remove('active'));
    document.querySelectorAll('.tab-content').forEach(c => c.classList.remove('active'));

    // Ставим активность на текущую
    btnElement.classList.add('active');
    document.getElementById(`tab-${tabName}`).classList.add('active');

    // Загружаем данные для нужной вкладки
    if (tabName === 'appointments') loadAppointments();
    if (tabName === 'services') loadServices();
    if (tabName === 'masters') loadMasters();
};

// ==========================================
// ВКЛАДКА 1: ЗАПИСИ
// ==========================================
async function loadAppointments() {
    const tbody = document.getElementById('tableBodyAppointments');
    tbody.innerHTML = '<tr><td colspan="6" style="text-align: center;">Загрузка...</td></tr>';

    try {
        const appointments = await ApiClient.getAdminAppointments();

        if (appointments.length === 0) {
            tbody.innerHTML = '<tr><td colspan="6" style="text-align: center; color: var(--text-muted);">Записей пока нет</td></tr>';
            return;
        }

        tbody.innerHTML = '';
        appointments.forEach(a => {
            const dateObj = new Date(a.startTime);
            const dateStr = dateObj.toLocaleDateString('ru-RU') + ' ' + dateObj.toLocaleTimeString('ru-RU', { hour: '2-digit', minute: '2-digit' });

            let actionsHtml = '';
            if (a.status === 'Pending') {
                actionsHtml = `
                    <button class="btn-small btn-accent" onclick="changeStatus('${a.id}', 'Confirmed')">Подтвердить</button>
                    <button class="btn-small btn-danger" onclick="changeStatus('${a.id}', 'Canceled')" style="margin-left: 5px;">Отменить</button>
                `;
            } else if (a.status === 'Confirmed') {
                actionsHtml = `<button class="btn-small btn-danger" onclick="changeStatus('${a.id}', 'Canceled')">Отменить</button>`;
            } else {
                actionsHtml = `<span style="color: var(--text-muted); font-size: 12px;">Нет действий</span>`;
            }

            tbody.innerHTML += `
                <tr>
                    <td style="font-family: var(--font-mono);">${dateStr}</td>
                    <td>
                        <div style="font-weight: bold;">${a.clientName}</div>
                        <div style="color: var(--text-muted); font-size: 12px;">${a.clientPhone}</div>
                    </td>
                    <td>${a.masterName}</td>
                    <td>${a.serviceName}</td>
                    <td><span class="status-badge status-${a.status}">${a.status}</span></td>
                    <td>${actionsHtml}</td>
                </tr>`;
        });
    } catch (e) {
        tbody.innerHTML = `<tr><td colspan="6" style="text-align: center; color: var(--danger);">Ошибка: ${e.message}</td></tr>`;
    }
}

window.changeStatus = async (id, status) => {
    try {
        await ApiClient.updateStatus(id, status);
        showToast('Статус обновлен');
        loadAppointments();
    } catch (e) {
        showToast(e.message, true);
    }
};

// ==========================================
// ВКЛАДКА 2: УСЛУГИ (CRUD)
// ==========================================
async function loadServices() {
    const tbody = document.getElementById('tableBodyServices');
    tbody.innerHTML = '<tr><td colspan="4" style="text-align: center;">Загрузка...</td></tr>';

    try {
        const services = await ApiClient.getServices();
        tbody.innerHTML = '';
        services.forEach(s => {
            tbody.innerHTML += `
                <tr>
                    <td>${s.name}</td>
                    <td>${s.price} ₽</td>
                    <td>${s.durationMinutes} мин</td>
                    <td>
                        <button class="btn-small btn-accent" onclick="editService('${s.id}', '${s.name}', ${s.price}, ${s.durationMinutes})">✎ Ред.</button>
                        <button class="btn-small btn-danger" onclick="deleteService('${s.id}')" style="margin-left: 5px;">✖ Удалить</button>
                    </td>
                </tr>`;
        });
    } catch (e) {
        showToast(e.message, true);
    }
}

window.addService = async () => {
    const name = prompt("Название услуги:");
    if (!name) return;
    const price = parseFloat(prompt("Цена (₽):", "1000"));
    const duration = parseInt(prompt("Длительность (минут):", "60"));

    try {
        await ApiClient.request('/services', {
            method: 'POST',
            body: JSON.stringify({ name, price, durationMinutes: duration })
        });
        showToast('Услуга добавлена');
        loadServices();
    } catch (e) {
        showToast(e.message, true);
    }
};

window.editService = async (id, oldName, oldPrice, oldDuration) => {
    const name = prompt("Название услуги:", oldName);
    if (!name) return;
    const price = parseFloat(prompt("Цена (₽):", oldPrice));
    const duration = parseInt(prompt("Длительность (минут):", oldDuration));

    try {
        await ApiClient.request(`/services/${id}`, {
            method: 'PUT',
            body: JSON.stringify({ name, price, durationMinutes: duration })
        });
        showToast('Услуга обновлена');
        loadServices();
    } catch (e) {
        showToast(e.message, true);
    }
};

window.deleteService = async (id) => {
    if (!confirm("Скрыть эту услугу? Клиенты больше не смогут на нее записаться.")) return;
    try {
        await ApiClient.request(`/services/${id}`, { method: 'DELETE' });
        showToast('Услуга скрыта');
        loadServices();
    } catch (e) {
        showToast(e.message, true);
    }
};

// ==========================================
// ВКЛАДКА 3: МАСТЕРА И ГРАФИК
// ==========================================
async function loadMasters() {
    const tbody = document.getElementById('tableBodyMasters');
    tbody.innerHTML = '<tr><td colspan="3" style="text-align: center;">Загрузка...</td></tr>';

    try {
        const masters = await ApiClient.getMasters();
        tbody.innerHTML = '';
        masters.forEach(m => {
            tbody.innerHTML += `
                <tr>
                    <td>${m.name}</td>
                    <td>${m.phone}</td>
                    <td>
                        <button class="btn-small btn-accent" onclick="openSchedule('${m.id}', '${m.name}')">🗓 График</button>
                        <button class="btn-small btn-accent" onclick="editMaster('${m.id}', '${m.name}', '${m.phone}')" style="margin-left: 5px;">✎ Ред.</button>
                        <button class="btn-small btn-danger" onclick="deleteMaster('${m.id}')" style="margin-left: 5px;">✖ Удалить</button>
                    </td>
                </tr>`;
        });
    } catch (e) {
        showToast(e.message, true);
    }
}

window.addMaster = async () => {
    const name = prompt("Имя мастера:");
    if (!name) return;
    const phone = prompt("Телефон:");

    try {
        await ApiClient.request('/masters', {
            method: 'POST',
            body: JSON.stringify({ name, phone })
        });
        showToast('Мастер добавлен');
        loadMasters();
    } catch (e) {
        showToast(e.message, true);
    }
};

window.editMaster = async (id, oldName, oldPhone) => {
    const name = prompt("Имя мастера:", oldName);
    if (!name) return;
    const phone = prompt("Телефон:", oldPhone);

    try {
        await ApiClient.request(`/masters/${id}`, {
            method: 'PUT',
            body: JSON.stringify({ name, phone })
        });
        showToast('Данные мастера обновлены');
        loadMasters();
    } catch (e) {
        showToast(e.message, true);
    }
};

window.deleteMaster = async (id) => {
    if (!confirm("Уволить мастера? Он исчезнет с сайта, но его старые записи останутся в истории.")) return;
    try {
        await ApiClient.request(`/masters/${id}`, { method: 'DELETE' });
        showToast('Мастер уволен (деактивирован)');
        loadMasters();
    } catch (e) {
        showToast(e.message, true);
    }
};

// ==========================================
// ГРАФИК РАБОТЫ (МОДАЛЬНОЕ ОКНО)
// ==========================================
let currentScheduleMasterId = null;
const DAYS_OF_WEEK = [
    { id: 1, name: "Понедельник" },
    { id: 2, name: "Вторник" },
    { id: 3, name: "Среда" },
    { id: 4, name: "Четверг" },
    { id: 5, name: "Пятница" },
    { id: 6, name: "Суббота" },
    { id: 0, name: "Воскресенье" }
];

window.openSchedule = async (masterId, masterName) => {
    currentScheduleMasterId = masterId;
    document.getElementById('scheduleMasterName').innerText = masterName;

    // Получаем текущий график мастера
    try {
        const schedule = await ApiClient.request(`/masters/${masterId}/schedule`);

        const container = document.getElementById('scheduleDays');
        container.innerHTML = '';

        DAYS_OF_WEEK.forEach(day => {
            // Ищем настройку для конкретного дня (если нет - ставим дефолт)
            const daySchedule = schedule.find(s => s.dayOfWeek === day.id) || {
                startTime: "10:00:00",
                endTime: "20:00:00",
                isDayOff: true
            };

            // Обрезаем секунды для инпута type="time"
            const startVal = daySchedule.startTime.substring(0, 5);
            const endVal = daySchedule.endTime.substring(0, 5);

            container.innerHTML += `
                <div class="schedule-row">
                    <input type="checkbox" id="day-${day.id}" ${!daySchedule.isDayOff ? 'checked' : ''} onchange="toggleDay(${day.id})">
                    <span>${day.name}</span>
                    <input type="time" id="start-${day.id}" value="${startVal}" ${daySchedule.isDayOff ? 'disabled' : ''}>
                    <input type="time" id="end-${day.id}" value="${endVal}" ${daySchedule.isDayOff ? 'disabled' : ''}>
                </div>
            `;
        });

        document.getElementById('scheduleModal').style.display = 'flex';
    } catch (e) {
        showToast("Ошибка загрузки графика", true);
    }
};

window.toggleDay = (dayId) => {
    const isChecked = document.getElementById(`day-${dayId}`).checked;
    document.getElementById(`start-${dayId}`).disabled = !isChecked;
    document.getElementById(`end-${dayId}`).disabled = !isChecked;
};

window.saveSchedule = async () => {
    const dtos = [];

    DAYS_OF_WEEK.forEach(day => {
        const isChecked = document.getElementById(`day-${day.id}`).checked;
        const start = document.getElementById(`start-${day.id}`).value;
        const end = document.getElementById(`end-${day.id}`).value;

        dtos.push({
            dayOfWeek: day.id,
            startTime: isChecked ? `${start}:00` : "00:00:00",
            endTime: isChecked ? `${end}:00` : "00:00:00",
            isDayOff: !isChecked
        });
    });

    try {
        await ApiClient.request(`/masters/${currentScheduleMasterId}/schedule`, {
            method: 'POST',
            body: JSON.stringify(dtos)
        });
        showToast('График сохранен');
        document.getElementById('scheduleModal').style.display = 'none';
    } catch (e) {
        showToast(e.message, true);
    }
};