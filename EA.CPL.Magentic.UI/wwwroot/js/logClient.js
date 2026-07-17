window.logClient = {
    connection: null,
    start: function (url, orchestrationId, dotNetRef) {
        this.connection = new signalR.HubConnectionBuilder()
            .withUrl(url)
            .withAutomaticReconnect()
            .build();

        this.connection.on("ReceiveLog", function (entry) {
            try {
                if (dotNetRef) {
                    dotNetRef.invokeMethodAsync('ReceiveLog', entry);
                }
            } catch (e) {
                console.error(e);
            }
        });

        return this.connection.start().then(function () {
            return this.connection.invoke('SubscribeToOrchestration', orchestrationId);
        });
    },
    stop: function (orchestrationId) {
        if (!this.connection) return Promise.resolve();
        return this.connection.invoke('UnsubscribeFromOrchestration', orchestrationId)
            .then(() => this.connection.stop());
    }
};
