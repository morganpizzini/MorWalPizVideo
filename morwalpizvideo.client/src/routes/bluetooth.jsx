import { useState, useEffect } from 'react';
import { Container, Button, Alert, Spinner, ListGroup, Badge, Card } from 'react-bootstrap';

const EVENT_SERVICE_UUID = '7520ffff-14d2-4cda-8b6b-697c554c9311';
//const EVENT_CHARACTERISTIC_UUID = '75200000-14d2-4cda-8b6b-697c554c9311';
const EVENT_CHARACTERISTIC_UUID = '75200001-14d2-4cda-8b6b-697c554c9311';

const eventNames = {
    0x00: 'SESSION_STARTED',
    0x01: 'SESSION_STOPPED',
    0x0A: 'DRYFIRE_REPEAT_BEGIN',
    0x0B: 'DRY_TIME_BEGIN',
    0x0C: 'DRY_TIME_END',
    0x14: 'SHOOTING_SET_BEGIN',
    0x15: 'SHOOTING_SET_END',
    0x1E: 'SHOT_DETECTED',
};

const BluetoothEventsPage = () => {
    const [device, setDevice] = useState(null);
    const [eventCharacteristic, setEventCharacteristic] = useState(null);
    const [isConnecting, setIsConnecting] = useState(false);
    const [isConnected, setIsConnected] = useState(false);
    const [messages, setMessages] = useState([]);
    const [error, setError] = useState(null);

    // Parsing degli eventi
    const parseEventData = (value) => {
        const eventId = value.getUint8(1);
        const eventName = eventNames[eventId] || `UNKNOWN_EVENT (0x${eventId.toString(16)})`;

        let message = eventName;

        switch (eventId) {
            case 0x1E: { // SHOT_DETECTED
                const shotNum = value.getUint16(2, true);
                const shotTime = value.getUint32(4, true);
                message = `SHOT_DETECTED ➡️ Colpo ${shotNum}, Tempo ${shotTime} ms`;
                break;
            }
            case 0x00: { // SESSION_STARTED
                message = `SESSION_STARTED ➡️ Sessione avviata`;
                break;
            }
            case 0x01: { // SESSION_STOPPED
                message = `SESSION_STOPPED ➡️ Sessione terminata`;
                break;
            }
            // Aggiungi altri case in base alle specifiche...
            default:
                message = `Evento ricevuto: ${eventName}`;
        }

        return message;
    };

    // Gestore delle notifiche
    const handleNotification = (event) => {
        const value = event.target.value;
        const message = parseEventData(value);

        setMessages(prev => [
            { text: message, timestamp: new Date().toLocaleTimeString() },
            ...prev,
        ]);
    };

    const handleDisconnect = () => {
        setIsConnected(false);
        setMessages(prev => [
            { text: 'Disconnesso dal dispositivo.', timestamp: new Date().toLocaleTimeString() },
            ...prev,
        ]);
        cleanupConnection();
    };

    const cleanupConnection = () => {
        if (eventCharacteristic) {
            eventCharacteristic.removeEventListener('characteristicvaluechanged', handleNotification);
            eventCharacteristic.stopNotifications().catch(console.error);
        }
        if (device && device.gatt.connected) {
            device.gatt.disconnect();
        }
        setDevice(null);
        setEventCharacteristic(null);
    };

    const connectDevice = async () => {
        setIsConnecting(true);
        setError(null);

        try {
            const bluetoothDevice = await navigator.bluetooth.requestDevice({
                acceptAllDevices: true,
                optionalServices: [EVENT_SERVICE_UUID],
                //filters: [{ services: [EVENT_SERVICE_UUID] }],
            });

            bluetoothDevice.addEventListener('gattserverdisconnected', handleDisconnect);
            setDevice(bluetoothDevice);

            //const server = await bluetoothDevice.gatt.connect();
            await bluetoothDevice.gatt.connect();
            const server = bluetoothDevice.gatt;
            console.log(server)
            const service = await server.getPrimaryService(EVENT_SERVICE_UUID);
            
            const characteristics = await service.getCharacteristics();
            console.log("Caratteristiche disponibili:");
            characteristics.forEach(c => console.log(c.uuid));

            console.log('ciao')
            const characteristic = await service.getCharacteristic(EVENT_CHARACTERISTIC_UUID);
            console.log(characteristic)

            await characteristic.startNotifications();
            console.log('ciao3')

            characteristic.addEventListener('characteristicvaluechanged', handleNotification);
            console.log('ciao4')

            setEventCharacteristic(characteristic);
            setIsConnected(true);
            setMessages(prev => [
                { text: 'Connesso e in ascolto eventi!', timestamp: new Date().toLocaleTimeString() },
                ...prev,
            ]);
        } catch (err) {
            console.error('Errore connessione Bluetooth:', err);
            setError(`Errore: ${err.message}`);
        } finally {
            setIsConnecting(false);
        }
    };

    const disconnectDevice = () => {
        if (device?.gatt?.connected) {
            device.gatt.disconnect();
            console.log('Dispositivo disconnesso.');
        } else {
            console.log('Nessun dispositivo connesso.');
        }
    };

    // Cleanup all'unmount
    useEffect(() => {
        return () => {
            cleanupConnection();
        };
    }, []);

    return (
        <Container className="my-5">
            <Card className="text-center">
                <Card.Header>🟦 Dispositivo BLE - Event Logger</Card.Header>
                <Card.Body>
                    <Card.Title>Stato Connessione:</Card.Title>
                    <h5>
                        {isConnected ? (
                            <Badge bg="success">Connesso</Badge>
                        ) : (
                            <Badge bg="secondary">Disconnesso</Badge>
                        )}
                    </h5>

                    <div className="my-3">
                        <Button
                            variant={isConnected ? 'secondary' : 'primary'}
                            onClick={connectDevice}
                            disabled={isConnected || isConnecting}
                        >
                            {isConnecting ? (
                                <>
                                    <Spinner as="span" animation="border" size="sm" role="status" className="me-2" />
                                    Connessione in corso...
                                </>
                            ) : (
                                isConnected ? 'Connesso' : 'Connetti Dispositivo'
                            )}
                        </Button>
                        {isConnected &&
                            <Button
                                variant={'danger'}
                                onClick={disconnectDevice}
                            >
                                Disconnetti
                            </Button>
                        }
                    </div>

                    {error && (
                        <Alert variant="danger" onClose={() => setError(null)} dismissible>
                            {error}
                        </Alert>
                    )}

                    <Card className="mt-4">
                        <Card.Header>📋 Log Eventi</Card.Header>
                        <ListGroup variant="flush" style={{ maxHeight: '300px', overflowY: 'auto' }}>
                            {messages.length === 0 ? (
                                <ListGroup.Item>Nessun evento rilevato.</ListGroup.Item>
                            ) : (
                                messages.map((msg, index) => (
                                    <ListGroup.Item key={index}>
                                        <div className="d-flex justify-content-between">
                                            <span>{msg.text}</span>
                                            <small className="text-muted">{msg.timestamp}</small>
                                        </div>
                                    </ListGroup.Item>
                                ))
                            )}
                        </ListGroup>
                    </Card>
                </Card.Body>
            </Card>
        </Container>
    );
};

export default BluetoothEventsPage;
