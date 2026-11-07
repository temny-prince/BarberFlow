let selectedTime = null;

document.addEventListener('DOMContentLoaded', async () => {
    // Устанавливаем дату на завтра
    const tomorrow = new Date();
    tomorrow.setDate(tomorrow.getDate() + 1);
    document.getElementById('dateInput').value = tomorrow.toISOString().split('T')[0];

    // Загружаем услуги и мастеров
    try {
        const [services, masters] = await Promise.all([
            ApiClient.getServices(),
            ApiClient.getMasters()
        ]);

        const serviceSelect = document.getElementById('serviceSelect');
        services.forEach(s => {
            serviceSelect.innerHTML += `<option value="${s.id}">${s.name} [${s.price}₽ / ${s.durationMinutes}м]</option>`;
        });

        const masterSelect = document.getElementById('masterSelect');
        masters.forEach(m => {
            masterSelect.innerHTML += `<option value="${m.id}">${m.name}</option>`;
        });
    } catch (e) {
        showToast('Ошибка загрузки данных', true);
    }
});

document.getElementById('btn-find-slots').addEventListener('click', async () => {
    const masterId = document.getElementById('masterSelect').value;
    const date = document.getElementById('dateInput').value;

    if (!masterId || !date) return showToast('Выберите мастера и дату', true);

    try {
        const slots = await ApiClient.getSlots(masterId, date);
        const container = document.getElementById('slotsContainer');
        container.innerHTML = '';
        selectedTime = null;
        document.getElementById('btn-book').disabled = true;

        if (slots.length === 0) {
            container.innerHTML = '<div style="grid-column: 1/-1; color: var(--text-muted); text-align: center; font-family: var(--font-mono);">[ Нет свободного времени ]</div>';
        } else {
            slots.forEach(time => {
                const div = document.createElement('div');
                div.className = 'slot';
                div.innerText = time;
                div.onclick = () => {
                    document.querySelectorAll('.slot').forEach(s => s.classList.remove('active'));
                    div.classList.add('active');
                    selectedTime = `${date}T${time}:00`;
                    document.getElementById('btn-book').disabled = false;
                };
                container.appendChild(div);
            });
        }
        document.getElementById('step-2').classList.remove('hidden');
    } catch (e) {
        showToast('Ошибка поиска слотов', true);
    }
});

document.getElementById('btn-book').addEventListener('click', async () => {
    const masterId = document.getElementById('masterSelect').value;
    const serviceId = document.getElementById('serviceSelect').value;
    const clientName = document.getElementById('clientName').value;
    const clientPhone = document.getElementById('clientPhone').value;
    const btn = document.getElementById('btn-book');

    if (!clientName || !clientPhone) return showToast('Заполните имя и телефон', true);

    btn.disabled = true;
    btn.innerText = 'Отправка...';

    try {
        await ApiClient.bookAppointment({ masterId, serviceId, clientName, clientPhone, startTime: selectedTime });
        showToast('Вы успешно записаны!');

        // Очистка и обновление слотов
        document.getElementById('clientName').value = '';
        document.getElementById('clientPhone').value = '';
        document.getElementById('step-2').classList.add('hidden');
    } catch (e) {
        showToast(e.message, true);
    } finally {
        btn.innerText = 'Подтвердить запись';
        btn.disabled = false;
    }
});