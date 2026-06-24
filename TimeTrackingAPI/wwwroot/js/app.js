const API_BASE = '/api';

// ==================== ОБЩИЕ ФУНКЦИИ ====================

async function apiRequest(endpoint, method = 'GET', data = null) {
    const options = {
        method: method,
        headers: {
            'Content-Type': 'application/json',
        }
    };

    if (data) {
        options.body = JSON.stringify(data);
    }

    try {
        const response = await fetch(`${API_BASE}${endpoint}`, options);

        if (!response.ok) {
            const error = await response.text();
            throw new Error(error || `HTTP error! status: ${response.status}`);
        }

        if (method === 'DELETE' || response.status === 204) {
            return null;
        }

        return await response.json();
    } catch (error) {
        console.error('API Error:', error);
        showToast('Ошибка: ' + error.message, 'error');
        throw error;
    }
}

function showToast(message, type = 'info') {
    const colors = {
        info: '#667eea',
        success: '#48bb78',
        error: '#fc8181',
        warning: '#f6ad55'
    };

    const toast = document.createElement('div');
    toast.style.cssText = `
        position: fixed;
        bottom: 20px;
        right: 20px;
        padding: 15px 25px;
        border-radius: 8px;
        color: white;
        background: ${colors[type] || colors.info};
        box-shadow: 0 5px 20px rgba(0,0,0,0.2);
        z-index: 9999;
        animation: slideDown 0.3s;
        max-width: 400px;
    `;
    toast.textContent = message;
    document.body.appendChild(toast);

    setTimeout(() => {
        toast.style.opacity = '0';
        toast.style.transition = 'opacity 0.3s';
        setTimeout(() => toast.remove(), 300);
    }, 3000);
}

function renderContent(html) {
    document.getElementById('mainContent').innerHTML = html;
}

function openModal(html) {
    document.getElementById('modalBody').innerHTML = html;
    document.getElementById('modal').style.display = 'block';
}

function closeModal() {
    document.getElementById('modal').style.display = 'none';
}

function formatDate(dateString) {
    const date = new Date(dateString);
    return date.toLocaleDateString('ru-RU');
}

function formatDateTime(dateString) {
    const date = new Date(dateString);
    return date.toLocaleString('ru-RU');
}

window.onclick = function (event) {
    const modal = document.getElementById('modal');
    if (event.target === modal) {
        closeModal();
    }
};

// ==================== ПРОЕКТЫ ====================

async function showAllProjects() {
    try {
        const projects = await apiRequest('/projects');
        renderProjectsList(projects);
    } catch (error) {
        console.error('Error loading projects:', error);
    }
}

function renderProjectsList(projects) {
    let html = `
        <div class="card">
            <div class="card-header">
                <h2 class="card-title">📋 Проекты</h2>
                <button class="btn btn-success" onclick="showCreateProjectForm()">+ Добавить проект</button>
            </div>
            <div class="table-responsive">
                <table>
                    <thead>
                        <tr>
                            <th>Код</th>
                            <th>Название</th>
                            <th>Статус</th>
                            <th>Действия</th>
                        </tr>
                    </thead>
                    <tbody>
    `;

    if (!projects || projects.length === 0) {
        html += `<tr><td colspan="4" style="text-align:center;padding:40px;">Нет проектов</td></tr>`;
    } else {
        projects.forEach(project => {
            html += `
                <tr>
                    <td><strong>${project.code}</strong></td>
                    <td>${project.name}</td>
                    <td>
                        <span class="sticker ${project.isActive ? 'sticker-green' : 'sticker-red'}">
                            ${project.isActive ? 'Активный' : 'Неактивный'}
                        </span>
                    </td>
                    <td>
                        <button class="btn btn-primary btn-sm" onclick="showEditProjectForm(${project.id})">✏️</button>
                        <button class="btn btn-danger btn-sm" onclick="deleteProject(${project.id})">🗑️</button>
                        <button class="btn btn-warning btn-sm" onclick="showProjectTasks(${project.id})">📝</button>
                    </td>
                </tr>
            `;
        });
    }

    html += `
                    </tbody>
                </table>
            </div>
        </div>
    `;

    renderContent(html);
}

async function showCreateProjectForm() {
    const html = `
        <h2>➕ Новый проект</h2>
        <form onsubmit="createProject(event)">
            <div class="form-group">
                <label>Код проекта *</label>
                <input type="text" id="projectCode" required placeholder="Например: WEB-01">
            </div>
            <div class="form-group">
                <label>Название *</label>
                <input type="text" id="projectName" required placeholder="Название проекта">
            </div>
            <div class="form-group">
                <label>
                    <input type="checkbox" id="projectActive" checked>
                    Активный
                </label>
            </div>
            <div style="display:flex;gap:10px;margin-top:20px;">
                <button type="submit" class="btn btn-success">Создать</button>
                <button type="button" class="btn btn-danger" onclick="closeModal()">Отмена</button>
            </div>
        </form>
    `;
    openModal(html);
}

async function createProject(event) {
    event.preventDefault();
    const data = {
        code: document.getElementById('projectCode').value.trim(),
        name: document.getElementById('projectName').value.trim(),
        isActive: document.getElementById('projectActive').checked
    };

    try {
        await apiRequest('/projects', 'POST', data);
        closeModal();
        showToast('Проект создан!', 'success');
        showAllProjects();
    } catch (error) {
        console.error('Error creating project:', error);
    }
}

async function showEditProjectForm(id) {
    try {
        const project = await apiRequest(`/projects/${id}`);
        const html = `
            <h2>✏️ Редактировать проект</h2>
            <form onsubmit="updateProject(${id}, event)">
                <div class="form-group">
                    <label>Код проекта *</label>
                    <input type="text" id="editProjectCode" required value="${project.code}">
                </div>
                <div class="form-group">
                    <label>Название *</label>
                    <input type="text" id="editProjectName" required value="${project.name}">
                </div>
                <div class="form-group">
                    <label>
                        <input type="checkbox" id="editProjectActive" ${project.isActive ? 'checked' : ''}>
                        Активный
                    </label>
                </div>
                <div style="display:flex;gap:10px;margin-top:20px;">
                    <button type="submit" class="btn btn-primary">Сохранить</button>
                    <button type="button" class="btn btn-danger" onclick="closeModal()">Отмена</button>
                </div>
            </form>
        `;
        openModal(html);
    } catch (error) {
        console.error('Error loading project:', error);
    }
}

async function updateProject(id, event) {
    event.preventDefault();
    const data = {
        code: document.getElementById('editProjectCode').value.trim(),
        name: document.getElementById('editProjectName').value.trim(),
        isActive: document.getElementById('editProjectActive').checked
    };

    try {
        await apiRequest(`/projects/${id}`, 'PUT', data);
        closeModal();
        showToast('Проект обновлен!', 'success');
        showAllProjects();
    } catch (error) {
        console.error('Error updating project:', error);
    }
}

async function deleteProject(id) {
    if (!confirm('Вы уверены, что хотите удалить этот проект?')) return;
    try {
        await apiRequest(`/projects/${id}`, 'DELETE');
        showToast('Проект удален!', 'success');
        showAllProjects();
    } catch (error) {
        console.error('Error deleting project:', error);
    }
}

async function showProjectTasks(projectId) {
    try {
        const tasks = await apiRequest(`/projects/${projectId}/tasks`);
        const project = await apiRequest(`/projects/${projectId}`);

        let html = `
            <div class="card">
                <div class="card-header">
                    <h2 class="card-title">📝 Задачи проекта: ${project.name}</h2>
                    <div>
                        <button class="btn btn-success btn-sm" onclick="showCreateTaskForm(${projectId})">+ Добавить задачу</button>
                        <button class="btn btn-primary btn-sm" onclick="showAllProjects()">← Назад</button>
                    </div>
                </div>
                <div class="table-responsive">
                    <table>
                        <thead>
                            <tr>
                                <th>Название</th>
                                <th>Статус</th>
                                <th>Действия</th>
                            </tr>
                        </thead>
                        <tbody>
        `;

        if (!tasks || tasks.length === 0) {
            html += `<tr><td colspan="3" style="text-align:center;padding:40px;">Нет задач в этом проекте</td></tr>`;
        } else {
            tasks.forEach(task => {
                html += `
                    <tr>
                        <td>${task.name}</td>
                        <td>
                            <span class="sticker ${task.isActive ? 'sticker-green' : 'sticker-red'}">
                                ${task.isActive ? 'Активная' : 'Неактивная'}
                            </span>
                        </td>
                        <td>
                            <button class="btn btn-primary btn-sm" onclick="showEditTaskForm(${task.id})">✏️</button>
                            <button class="btn btn-danger btn-sm" onclick="deleteTask(${task.id})">🗑️</button>
                        </td>
                    </tr>
                `;
            });
        }

        html += `
                        </tbody>
                    </table>
                </div>
            </div>
        `;

        renderContent(html);
    } catch (error) {
        console.error('Error loading project tasks:', error);
    }
}

// ==================== ЗАДАЧИ ====================

async function showAllTasks() {
    try {
        const tasks = await apiRequest('/tasks');
        renderTasksList(tasks);
    } catch (error) {
        console.error('Error loading tasks:', error);
    }
}

function renderTasksList(tasks) {
    let html = `
        <div class="card">
            <div class="card-header">
                <h2 class="card-title">📝 Все задачи</h2>
                <div>
                    <button class="btn btn-success btn-sm" onclick="showCreateTaskForm()">+ Добавить задачу</button>
                    <button class="btn btn-primary btn-sm" onclick="showAllProjects()">← Назад</button>
                </div>
            </div>
            <div class="table-responsive">
                <table>
                    <thead>
                        <tr>
                            <th>Название</th>
                            <th>Проект</th>
                            <th>Статус</th>
                            <th>Действия</th>
                        </tr>
                    </thead>
                    <tbody>
    `;

    if (!tasks || tasks.length === 0) {
        html += `<tr><td colspan="4" style="text-align:center;padding:40px;">Нет задач</td></tr>`;
    } else {
        tasks.forEach(task => {
            html += `
                <tr>
                    <td>${task.name}</td>
                    <td>${task.projectName || 'Без проекта'}</td>
                    <td>
                        <span class="sticker ${task.isActive ? 'sticker-green' : 'sticker-red'}">
                            ${task.isActive ? 'Активная' : 'Неактивная'}
                        </span>
                    </td>
                    <td>
                        <button class="btn btn-primary btn-sm" onclick="showEditTaskForm(${task.id})">✏️</button>
                        <button class="btn btn-danger btn-sm" onclick="deleteTask(${task.id})">🗑️</button>
                    </td>
                </tr>
            `;
        });
    }

    html += `
                    </tbody>
                </table>
            </div>
        </div>
    `;

    renderContent(html);
}

async function showCreateTaskForm(projectId = null) {
    try {
        const projects = await apiRequest('/projects');

        let html = `
            <h2>➕ Новая задача</h2>
            <form onsubmit="createTask(event)">
                <div class="form-group">
                    <label>Название задачи *</label>
                    <input type="text" id="taskName" required placeholder="Название задачи">
                </div>
                <div class="form-group">
                    <label>Проект *</label>
                    <select id="taskProjectId" required>
                        <option value="">Выберите проект</option>
        `;

        projects.forEach(p => {
            const selected = p.id === projectId ? 'selected' : '';
            html += `<option value="${p.id}" ${selected}>${p.name} (${p.code})</option>`;
        });

        html += `
                    </select>
                </div>
                <div class="form-group">
                    <label>
                        <input type="checkbox" id="taskActive" checked>
                        Активная
                    </label>
                </div>
                <div style="display:flex;gap:10px;margin-top:20px;">
                    <button type="submit" class="btn btn-success">Создать</button>
                    <button type="button" class="btn btn-danger" onclick="closeModal()">Отмена</button>
                </div>
            </form>
        `;
        openModal(html);
    } catch (error) {
        console.error('Error loading projects for task creation:', error);
    }
}

async function createTask(event) {
    event.preventDefault();
    const data = {
        name: document.getElementById('taskName').value.trim(),
        projectId: parseInt(document.getElementById('taskProjectId').value),
        isActive: document.getElementById('taskActive').checked
    };

    try {
        await apiRequest('/tasks', 'POST', data);
        closeModal();
        showToast('Задача создана!', 'success');
        showAllTasks();
    } catch (error) {
        console.error('Error creating task:', error);
    }
}

async function showEditTaskForm(id) {
    try {
        const task = await apiRequest(`/tasks/${id}`);
        const projects = await apiRequest('/projects');

        let html = `
            <h2>✏️ Редактировать задачу</h2>
            <form onsubmit="updateTask(${id}, event)">
                <div class="form-group">
                    <label>Название задачи *</label>
                    <input type="text" id="editTaskName" required value="${task.name}">
                </div>
                <div class="form-group">
                    <label>Проект *</label>
                    <select id="editTaskProjectId" required>
        `;

        projects.forEach(p => {
            const selected = p.id === task.projectId ? 'selected' : '';
            html += `<option value="${p.id}" ${selected}>${p.name} (${p.code})</option>`;
        });

        html += `
                    </select>
                </div>
                <div class="form-group">
                    <label>
                        <input type="checkbox" id="editTaskActive" ${task.isActive ? 'checked' : ''}>
                        Активная
                    </label>
                </div>
                <div style="display:flex;gap:10px;margin-top:20px;">
                    <button type="submit" class="btn btn-primary">Сохранить</button>
                    <button type="button" class="btn btn-danger" onclick="closeModal()">Отмена</button>
                </div>
            </form>
        `;
        openModal(html);
    } catch (error) {
        console.error('Error loading task:', error);
    }
}

async function updateTask(id, event) {
    event.preventDefault();
    const data = {
        name: document.getElementById('editTaskName').value.trim(),
        projectId: parseInt(document.getElementById('editTaskProjectId').value),
        isActive: document.getElementById('editTaskActive').checked
    };

    try {
        await apiRequest(`/tasks/${id}`, 'PUT', data);
        closeModal();
        showToast('Задача обновлена!', 'success');
        showAllTasks();
    } catch (error) {
        console.error('Error updating task:', error);
    }
}

async function deleteTask(id) {
    if (!confirm('Вы уверены, что хотите удалить эту задачу?')) return;
    try {
        await apiRequest(`/tasks/${id}`, 'DELETE');
        showToast('Задача удалена!', 'success');
        showAllTasks();
    } catch (error) {
        console.error('Error deleting task:', error);
    }
}

// ==================== ПРОВОДКИ ====================

async function showAllTimeEntries() {
    try {
        const entries = await apiRequest('/timeentries');
        renderTimeEntries(entries);
    } catch (error) {
        console.error('Error loading time entries:', error);
    }
}

async function renderTimeEntries(entries, date = null, month = null) {
    // Сортируем записи по дате (сначала новые)
    if (entries) {
        entries.sort((a, b) => new Date(b.entryDate) - new Date(a.entryDate));
    }

    let html = `
        <div class="card">
            <div class="card-header">
                <h2 class="card-title">⏱ Проводки</h2>
                <div>
                    <button class="btn btn-success btn-sm" onclick="showCreateTimeEntryForm()">+ Добавить проводку</button>
                </div>
            </div>
            
            <div class="filters">
                <div class="form-group">
                    <label>Поиск за день</label>
                    <input type="date" id="filterDate" value="${date || ''}" onchange="filterByDate()">
                </div>
                <div class="form-group">
                    <label>Поиск за месяц</label>
                    <input type="month" id="filterMonth" value="${month || ''}" onchange="filterByMonth()">
                </div>
                <div>
                    <button class="btn btn-primary btn-sm" onclick="showAllTimeEntries()">Сбросить</button>
                </div>
            </div>
    `;

    // ✅ ВСЕГДА ПОКАЗЫВАЕМ ОБЩЕЕ КОЛИЧЕСТВО ЧАСОВ
    if (entries && entries.length > 0) {
        const totalHours = entries.reduce((sum, e) => sum + e.hours, 0);

        let statusHtml = '';
        // ✅ СТИКЕР ПОКАЗЫВАЕМ ТОЛЬКО ДЛЯ КОНКРЕТНОГО ДНЯ
        if (date) {
            const status = totalHours < 8 ? 'Yellow' : totalHours === 8 ? 'Green' : 'Red';
            const statusText = status === 'Yellow' ? '⚠️ Недостаточно' :
                status === 'Green' ? '✅ Достаточно' : '🔴 Избыточно';
            statusHtml = `
                <div>
                    <span class="sticker sticker-${status.toLowerCase()}">${statusText}</span>
                </div>
            `;
        }

        html += `
            <div class="summary-card">
                <div class="summary-info">
                    <div>
                        <div style="font-size:14px;color:#666;">Всего часов за период</div>
                        <div class="summary-hours">${totalHours.toFixed(1)} ч</div>
                    </div>
                    ${statusHtml}
                </div>
                <div style="font-size:14px;color:#666;">
                    Записей: ${entries.length}
                </div>
            </div>
        `;
    }

    html += `
            <div class="table-responsive">
                <table>
                    <thead>
                        <tr>
                            <th>Дата</th>
                            <th>Часы</th>
                            <th>Описание</th>
                            <th>Задача</th>
                            <th>Проект</th>
                            <th>Действия</th>
                        </tr>
                    </thead>
                    <tbody>
    `;

    if (!entries || entries.length === 0) {
        html += `<tr><td colspan="6" style="text-align:center;padding:40px;">Нет проводок</td></tr>`;
    } else {
        entries.forEach(entry => {
            html += `
                <tr>
                    <td>${formatDate(entry.entryDate)}</td>
                    <td><strong>${entry.hours.toFixed(1)} ч</strong></td>
                    <td>${entry.description || '-'}</td>
                    <td>${entry.taskName || '-'}</td>
                    <td>${entry.projectName || '-'}</td>
                    <td>
                        <button class="btn btn-primary btn-sm" onclick="showEditTimeEntryForm(${entry.id})">✏️</button>
                        <button class="btn btn-danger btn-sm" onclick="deleteTimeEntry(${entry.id})">🗑️</button>
                    </td>
                </tr>
            `;
        });
    }

    html += `
                    </tbody>
                </table>
            </div>
        </div>
    `;

    renderContent(html);
}

async function filterByDate() {
    const date = document.getElementById('filterDate').value;
    if (!date) return;
    try {
        const entries = await apiRequest(`/timeentries/by-date?date=${date}`);
        renderTimeEntries(entries, date);
    } catch (error) {
        console.error('Error filtering by date:', error);
    }
}

async function filterByMonth() {
    const month = document.getElementById('filterMonth').value;
    if (!month) return;
    try {
        const entries = await apiRequest(`/timeentries/by-month?month=${month}`);
        renderTimeEntries(entries, null, month);
    } catch (error) {
        console.error('Error filtering by month:', error);
    }
}

async function showCreateTimeEntryForm() {
    try {
        const tasks = await apiRequest('/tasks/active');

        let html = `
            <h2>➕ Новая проводка</h2>
            <form onsubmit="createTimeEntry(event)">
                <div class="form-group">
                    <label>Дата *</label>
                    <input type="date" id="timeEntryDate" required value="${new Date().toISOString().split('T')[0]}">
                </div>
                <div class="form-group">
                    <label>Количество часов * (0.1 - 24)</label>
                    <input type="number" id="timeEntryHours" required 
                           min="0.1" max="24" step="0.1" 
                           value="1" 
                           placeholder="Например: 8"
                           onchange="validateHours(this)">
                </div>
                <div class="form-group">
                    <label>Задача *</label>
                    <select id="timeEntryTaskId" required>
                        <option value="">Выберите задачу</option>
        `;

        tasks.forEach(t => {
            html += `<option value="${t.id}">${t.name} (${t.projectName || 'Без проекта'})</option>`;
        });

        html += `
                    </select>
                </div>
                <div class="form-group">
                    <label>Описание</label>
                    <input type="text" id="timeEntryDescription" placeholder="Краткое описание работы">
                </div>
                <div style="display:flex;gap:10px;margin-top:20px;">
                    <button type="submit" class="btn btn-success">Создать</button>
                    <button type="button" class="btn btn-danger" onclick="closeModal()">Отмена</button>
                </div>
            </form>
        `;
        openModal(html);
    } catch (error) {
        console.error('Error loading tasks for time entry:', error);
    }
}

// Добавьте эту функцию для валидации часов
function validateHours(input) {
    let value = parseFloat(input.value);
    if (isNaN(value) || value < 0.1) {
        input.value = 0.1;
    } else if (value > 24) {
        input.value = 24;
    }
    // Округляем до 1 знака после запятой
    input.value = Math.round(parseFloat(input.value) * 10) / 10;
}

async function createTimeEntry(event) {
    event.preventDefault();
    const data = {
        entryDate: document.getElementById('timeEntryDate').value,
        hours: parseFloat(document.getElementById('timeEntryHours').value),
        description: document.getElementById('timeEntryDescription').value.trim(),
        taskId: parseInt(document.getElementById('timeEntryTaskId').value)
    };

    try {
        await apiRequest('/timeentries', 'POST', data);
        closeModal();
        showToast('Проводка создана!', 'success');
        showAllTimeEntries();
    } catch (error) {
        console.error('Error creating time entry:', error);
    }
}

async function showEditTimeEntryForm(id) {
    try {
        const entry = await apiRequest(`/timeentries/${id}`);
        const tasks = await apiRequest('/tasks/active');

        let html = `
            <h2>✏️ Редактировать проводку</h2>
            <form onsubmit="updateTimeEntry(${id}, event)">
                <div class="form-group">
                    <label>Дата *</label>
                    <input type="date" id="editTimeEntryDate" required value="${entry.entryDate.split('T')[0]}">
                </div>
                <div class="form-group">
                    <label>Количество часов * (0.1 - 24)</label>
                    <input type="number" id="editTimeEntryHours" required 
                           min="0.1" max="24" step="0.1" 
                           value="${entry.hours}"
                           onchange="validateHours(this)">
                </div>
                <div class="form-group">
                    <label>Задача *</label>
                    <select id="editTimeEntryTaskId" required>
        `;

        tasks.forEach(t => {
            const selected = t.id === entry.taskId ? 'selected' : '';
            html += `<option value="${t.id}" ${selected}>${t.name} (${t.projectName || 'Без проекта'})</option>`;
        });

        html += `
                    </select>
                </div>
                <div class="form-group">
                    <label>Описание</label>
                    <input type="text" id="editTimeEntryDescription" value="${entry.description || ''}">
                </div>
                <div style="display:flex;gap:10px;margin-top:20px;">
                    <button type="submit" class="btn btn-primary">Сохранить</button>
                    <button type="button" class="btn btn-danger" onclick="closeModal()">Отмена</button>
                </div>
            </form>
        `;
        openModal(html);
    } catch (error) {
        console.error('Error loading time entry:', error);
    }
}

async function updateTimeEntry(id, event) {
    event.preventDefault();
    const data = {
        entryDate: document.getElementById('editTimeEntryDate').value,
        hours: parseFloat(document.getElementById('editTimeEntryHours').value),
        description: document.getElementById('editTimeEntryDescription').value.trim(),
        taskId: parseInt(document.getElementById('editTimeEntryTaskId').value)
    };

    try {
        await apiRequest(`/timeentries/${id}`, 'PUT', data);
        closeModal();
        showToast('Проводка обновлена!', 'success');
        showAllTimeEntries();
    } catch (error) {
        console.error('Error updating time entry:', error);
    }
}

async function deleteTimeEntry(id) {
    if (!confirm('Вы уверены, что хотите удалить эту проводку?')) return;
    try {
        await apiRequest(`/timeentries/${id}`, 'DELETE');
        showToast('Проводка удалена!', 'success');
        showAllTimeEntries();
    } catch (error) {
        console.error('Error deleting time entry:', error);
    }
}

// ==================== ИНИЦИАЛИЗАЦИЯ ====================

document.addEventListener('DOMContentLoaded', function () {
    console.log('Time Tracking App загружен!');
});