(() => {
    const maximumRetryCount = 3;
    const retryIntervalMilliseconds = 1000;
    const reconnectModal = document.getElementById('reconnect-modal');

    reconnectModal.innerHTML =
        '<div class="p-1 text-bg-light">' +
            '<p>The connection to the Squidlr server is lost. Functionality may be affected.</p>' +
            '<p id="reconnect-modal-text"></p>' +
            '<p id="reload-text" style="display: none">Reconnecting was not successful. Click <span id="reload-link" style="text-decoration: underline; cursor: pointer;">here</span> to reload the page.</p>' +
        '</div >';

    const reconnectModalText = document.getElementById('reconnect-modal-text');

    const startReconnectionProcess = () => {
        let isCanceled = false;

        (async () => {
            for (let i = 0; i < maximumRetryCount; i++) {
                reconnectModalText.innerText = `Attempting to reconnect: ${i + 1} of ${maximumRetryCount}`;

                await new Promise(resolve => setTimeout(resolve, retryIntervalMilliseconds));
                reconnectModal.style.display = 'block';

                if (isCanceled) {
                    return;
                }

                try {
                    const result = await Blazor.reconnect();
                    if (!result) {
                        // The server was reached, but the connection was rejected; reload the page.
                        location.reload();
                        return;
                    }

                    // Successfully reconnected to the server.
                    return;
                } catch {
                    // Didn't reach the server; try again.
                }
            }

            // Retried too many times; give the user the choice to reload the page.
            reconnectModalText.style.display = 'none';
            const reloadText = document.getElementById('reload-text');
            reloadText.style.display = 'block';

            document.getElementById('reload-link').addEventListener('click', function () {
                location.reload();
            });
        })();

        return {
            cancel: () => {
                isCanceled = true;
                reconnectModal.style.display = 'none';
            },
        };
    };

    let currentReconnectionProcess = null;

    Blazor.start({
        reconnectionHandler: {
            onConnectionDown: () => currentReconnectionProcess ??= startReconnectionProcess(),
            onConnectionUp: () => {
                currentReconnectionProcess?.cancel();
                currentReconnectionProcess = null;
            },
        },
    });
})();