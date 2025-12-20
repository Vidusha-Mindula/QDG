$(document).ready(function() {
    $('#testSourceBtn').on('click', async function() {
        await testConnection('source');
    });

    $('#testDestBtn').on('click', async function() {
        await testConnection('destination');
    });

    async function testConnection(type) {
        const isSource = type === 'source';
        const prefix = isSource ? 'Source' : 'Destination';
        const $btn = isSource ? $('#testSourceBtn') : $('#testDestBtn');
        const $spinner = isSource ? $('#sourceSpinner') : $('#destSpinner');
        const $result = isSource ? $('#sourceResult') : $('#destResult');

        const data = {
            host: $(`#${prefix}Host`).val(),
            port: parseInt($(`#${prefix}Port`).val()),
            database: $(`#${prefix}Database`).val(),
            username: $(`#${prefix}Username`).val(),
            password: $(`#${prefix}Password`).val()
        };

        $spinner.removeClass('d-none');
        $btn.prop('disabled', true);
        $result.html('');

        try {
            const response = await fetch('/api/configuration/test-connection', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                },
                body: JSON.stringify(data)
            });

            const result = await response.json();

            if (result.success) {
                $result.html(`<div class="alert alert-success">
                    <i class="bi bi-check-circle"></i> ${result.message}
                </div>`);
            } else {
                $result.html(`<div class="alert alert-danger">
                    <i class="bi bi-x-circle"></i> ${result.message}
                </div>`);
            }
        } catch (error) {
            $result.html(`<div class="alert alert-danger">
                <i class="bi bi-x-circle"></i> Error: ${error.message}
            </div>`);
        } finally {
            $spinner.addClass('d-none');
            $btn.prop('disabled', false);
        }
    }
});
