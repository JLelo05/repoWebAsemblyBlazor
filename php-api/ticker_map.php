<?php
header('Content-Type: application/json; charset=utf-8');
header('Access-Control-Allow-Origin: *');
header('Access-Control-Allow-Methods: GET, POST, OPTIONS');
header('Access-Control-Allow-Headers: Content-Type');

if ($_SERVER['REQUEST_METHOD'] === 'OPTIONS') {
    http_response_code(204);
    exit;
}

require_once __DIR__ . '/config.php';

$conn = getDbConnection();
$method = $_SERVER['REQUEST_METHOD'];

if ($method === 'GET') {
    $usedTicker = trim($_GET['usedTicker'] ?? '');

    if ($usedTicker === '') {
        http_response_code(400);
        echo json_encode(['error' => 'usedTicker is required']);
        $conn->close();
        exit;
    }

    $stmt = $conn->prepare('SELECT XtbTicker, YahooTicker FROM TicketConversionTb WHERE XtbTicker = ? LIMIT 1');
    $stmt->bind_param('s', $usedTicker);
    $stmt->execute();
    $result = $stmt->get_result();

    $row = $result->fetch_assoc();
    $stmt->close();

    if ($row === null) {
        echo json_encode(['usedTicker' => $usedTicker, 'yahooTicker' => null]);
    } else {
        echo json_encode([
            'usedTicker' => $row['XtbTicker'],
            'yahooTicker' => $row['YahooTicker'],
        ]);
    }

    $conn->close();
    exit;
}

if ($method === 'POST') {
    $body = json_decode(file_get_contents('php://input'), true);
    $usedTicker = trim((string)($body['usedTicker'] ?? ''));
    $yahooTicker = trim((string)($body['yahooTicker'] ?? ''));

    if ($usedTicker === '' || $yahooTicker === '') {
        http_response_code(400);
        echo json_encode(['error' => 'usedTicker and yahooTicker are required']);
        $conn->close();
        exit;
    }

    $stmt = $conn->prepare('INSERT INTO TicketConversionTb (XtbTicker, YahooTicker) VALUES (?, ?) ON DUPLICATE KEY UPDATE YahooTicker = VALUES(YahooTicker)');
    $stmt->bind_param('ss', $usedTicker, $yahooTicker);
    $stmt->execute();
    $stmt->close();

    http_response_code(201);
    echo json_encode([
        'usedTicker' => $usedTicker,
        'yahooTicker' => $yahooTicker,
    ]);

    $conn->close();
    exit;
}

http_response_code(405);
echo json_encode(['error' => 'Method not allowed']);
$conn->close();
