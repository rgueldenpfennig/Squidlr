(() => {
    const maximumRetryCount = 3;
    const retryIntervalMilliseconds = 1000;
    const reconnectModal = document.getElementById('reconnect-modal');

    reconnectModal.innerHTML =
        '<div class="bg-danger" role="alert">' +
            '<h2>Oh no! The connection to the Squidlr server is lost :-(</h2>' +
            '<p id="reconnect-modal-text" class="fs-4"></p>' +
        '</div >';

    const reconnectModalText = document.getElementById('reconnect-modal-text');

    const startReconnectionProcess = () => {
        reconnectModal.style.display = 'block';

        let isCanceled = false;

        (async () => {
            for (let i = 0; i < maximumRetryCount; i++) {
                reconnectModalText.innerText = `Attempting to reconnect to the Squidlr server: ${i + 1} of ${maximumRetryCount}`;

                await new Promise(resolve => setTimeout(resolve, retryIntervalMilliseconds));

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

            // Retried too many times; reload the page.
            location.reload();
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