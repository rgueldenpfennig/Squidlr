(() => {
    const root = document.documentElement;

    const isDarkMode = () => root.getAttribute('data-bs-theme') === 'dark';

    const render = () => {
        const dark = isDarkMode();
        const icon = document.getElementById('themeIcon');
        const label = document.getElementById('themeLabel');
        if (icon) {
            icon.classList.toggle('oi-moon', dark);
            icon.classList.toggle('oi-sun', !dark);
        }
        if (label) {
            label.textContent = dark ? 'Dark' : 'Light';
        }
    };

    const setTheme = (isDark) => {
        const theme = isDark ? 'dark' : 'light';
        root.setAttribute('data-bs-theme', theme);
        try {
            localStorage.setItem('theme', theme);
        } catch {
            // storage unavailable (private mode etc.) - theme just won't persist
        }
        render();
    };

    const toggle = document.getElementById('themeSwitch');
    if (toggle) {
        toggle.addEventListener('click', () => setTheme(!isDarkMode()));
        toggle.addEventListener('keydown', (e) => {
            if (e.key === 'Enter' || e.key === ' ') {
                e.preventDefault();
                setTheme(!isDarkMode());
            }
        });
    }

    render();
})();
