self.addEventListener('push', function (event) {
    let data = { title: 'غرفة الماتش', body: '' };
    try {
        data = event.data.json();
    } catch (e) {
        if (event.data) data.body = event.data.text();
    }

    const options = {
        body: data.body || '',
        dir: 'rtl',
        lang: 'ar'
    };

    event.waitUntil(
        self.registration.showNotification(data.title || 'غرفة الماتش', options)
    );
});

self.addEventListener('notificationclick', function (event) {
    event.notification.close();
    event.waitUntil(
        clients.matchAll({ type: 'window' }).then(function (clientList) {
            for (const client of clientList) {
                if ('focus' in client) return client.focus();
            }
            if (clients.openWindow) return clients.openWindow('/Home/Room');
        })
    );
});
