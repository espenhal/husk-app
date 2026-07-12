const state = {
  tasks: [],
  filter: 'active',
  recognition: null,
  isListening: false
};

const elements = {
  addButton: document.querySelector('#addButton'),
  connectionStatus: document.querySelector('#connectionStatus'),
  emptyState: document.querySelector('#emptyState'),
  filterButtons: document.querySelectorAll('.filter-button'),
  micButton: document.querySelector('#micButton'),
  speechStatus: document.querySelector('#speechStatus'),
  taskInput: document.querySelector('#taskInput'),
  taskList: document.querySelector('#taskList')
};

elements.addButton.addEventListener('click', addTask);
elements.taskInput.addEventListener('keydown', event => {
  if ((event.metaKey || event.ctrlKey) && event.key === 'Enter') {
    addTask();
  }
});

for (const button of elements.filterButtons) {
  button.addEventListener('click', () => {
    state.filter = button.dataset.filter;
    updateFilterButtons();
    renderTasks();
  });
}

setupSpeechRecognition();
loadTasks();

async function loadTasks() {
  try {
    const response = await fetch('/api/tasks');
    ensureOk(response);
    state.tasks = await response.json();
    setConnectionStatus(true);
    renderTasks();
  } catch (error) {
    setConnectionStatus(false);
    setSpeechStatus('Kunne ikke hente oppgaver.');
  }
}

async function addTask() {
  const title = elements.taskInput.value.trim();

  if (!title) {
    elements.taskInput.focus();
    return;
  }

  elements.addButton.disabled = true;

  try {
    const response = await fetch('/api/tasks', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ title })
    });

    ensureOk(response);
    const created = await response.json();
    state.tasks = [created, ...state.tasks];
    elements.taskInput.value = '';
    setConnectionStatus(true);
    setSpeechStatus('Lagt til.');
    renderTasks();
  } catch (error) {
    setConnectionStatus(false);
    setSpeechStatus('Kunne ikke lagre oppgaven.');
  } finally {
    elements.addButton.disabled = false;
    elements.taskInput.focus();
  }
}

async function updateTask(id, changes) {
  const existing = state.tasks.find(task => task.id === id);

  if (!existing) {
    return;
  }

  const optimistic = { ...existing, ...changes };
  state.tasks = state.tasks.map(task => task.id === id ? optimistic : task);
  renderTasks();

  try {
    const response = await fetch(`/api/tasks/${id}`, {
      method: 'PUT',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(changes)
    });

    ensureOk(response);
    const updated = await response.json();
    state.tasks = state.tasks.map(task => task.id === id ? updated : task);
    setConnectionStatus(true);
    renderTasks();
  } catch (error) {
    state.tasks = state.tasks.map(task => task.id === id ? existing : task);
    setConnectionStatus(false);
    setSpeechStatus('Kunne ikke oppdatere oppgaven.');
    renderTasks();
  }
}

async function deleteTask(id) {
  const previous = state.tasks;
  state.tasks = state.tasks.filter(task => task.id !== id);
  renderTasks();

  try {
    const response = await fetch(`/api/tasks/${id}`, { method: 'DELETE' });
    ensureOk(response);
    setConnectionStatus(true);
  } catch (error) {
    state.tasks = previous;
    setConnectionStatus(false);
    setSpeechStatus('Kunne ikke slette oppgaven.');
    renderTasks();
  }
}

function renderTasks() {
  const visibleTasks = state.tasks.filter(task => {
    if (state.filter === 'active') {
      return !task.isCompleted;
    }

    if (state.filter === 'done') {
      return task.isCompleted;
    }

    return true;
  });

  elements.taskList.replaceChildren(...visibleTasks.map(createTaskRow));
  elements.emptyState.hidden = visibleTasks.length > 0;
}

function createTaskRow(task) {
  const item = document.createElement('li');
  item.className = `task-row${task.isCompleted ? ' is-completed' : ''}`;

  const checkbox = document.createElement('input');
  checkbox.type = 'checkbox';
  checkbox.checked = task.isCompleted;
  checkbox.ariaLabel = task.isCompleted ? 'Marker som aktiv' : 'Marker som ferdig';
  checkbox.addEventListener('change', () => updateTask(task.id, { isCompleted: checkbox.checked }));

  const title = document.createElement('span');
  title.className = 'task-title';
  title.textContent = task.title;

  const deleteButton = document.createElement('button');
  deleteButton.className = 'delete-button';
  deleteButton.type = 'button';
  deleteButton.ariaLabel = 'Slett';
  deleteButton.textContent = '×';
  deleteButton.addEventListener('click', () => deleteTask(task.id));

  item.append(checkbox, title, deleteButton);
  return item;
}

function setupSpeechRecognition() {
  const SpeechRecognition = window.SpeechRecognition || window.webkitSpeechRecognition;

  if (!SpeechRecognition || !window.isSecureContext) {
    elements.micButton.disabled = true;
    setSpeechStatus(window.isSecureContext ? 'Tale er ikke støttet i denne nettleseren.' : 'Mikrofon krever HTTPS.');
    return;
  }

  const recognition = new SpeechRecognition();
  recognition.lang = 'nb-NO';
  recognition.continuous = false;
  recognition.interimResults = true;

  recognition.addEventListener('start', () => {
    state.isListening = true;
    elements.micButton.classList.add('is-listening');
    setSpeechStatus('Lytter.');
  });

  recognition.addEventListener('result', event => {
    const transcript = Array
      .from(event.results)
      .map(result => result[0].transcript)
      .join(' ')
      .trim();

    elements.taskInput.value = transcript;
  });

  recognition.addEventListener('end', () => {
    state.isListening = false;
    elements.micButton.classList.remove('is-listening');
    setSpeechStatus(elements.taskInput.value.trim() ? 'Klar.' : '');
  });

  recognition.addEventListener('error', () => {
    state.isListening = false;
    elements.micButton.classList.remove('is-listening');
    setSpeechStatus('Kunne ikke bruke mikrofonen.');
  });

  elements.micButton.addEventListener('click', () => {
    if (state.isListening) {
      recognition.stop();
      return;
    }

    recognition.start();
  });

  state.recognition = recognition;
}

function updateFilterButtons() {
  for (const button of elements.filterButtons) {
    button.classList.toggle('is-active', button.dataset.filter === state.filter);
  }
}

function setConnectionStatus(isOnline) {
  elements.connectionStatus.textContent = isOnline ? 'Tilkoblet' : 'Frakoblet';
  elements.connectionStatus.classList.toggle('is-online', isOnline);
  elements.connectionStatus.classList.toggle('is-offline', !isOnline);
}

function setSpeechStatus(message) {
  elements.speechStatus.textContent = message;
}

function ensureOk(response) {
  if (!response.ok) {
    throw new Error(`Request failed with status ${response.status}`);
  }
}
