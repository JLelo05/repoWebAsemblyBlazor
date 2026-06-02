<?php
header('Content-Type: application/json; charset=utf-8');
header('Access-Control-Allow-Origin: *');
header('Access-Control-Allow-Methods: GET, POST, OPTIONS');
header('Access-Control-Allow-Headers: Content-Type');

// Handle CORS preflight
if ($_SERVER['REQUEST_METHOD'] === 'OPTIONS') {
    http_response_code(204);
    exit;
}

require_once __DIR__ . '/config.php';

$conn   = getDbConnection();
$method = $_SERVER['REQUEST_METHOD'];

if ($method === 'GET') {
    $result  = $conn->query('SELECT Name, Review, DateTime FROM Reviews ORDER BY DateTime DESC');
    $reviews = [];
    while ($row = $result->fetch_assoc()) {
        $reviews[] = [
            'name'      => $row['Name'],
            'text'      => $row['Review'],
            'createdAt' => date('Y-m-d\TH:i:s', strtotime($row['DateTime'])),
        ];
    }
    echo json_encode($reviews);

} elseif ($method === 'POST') {
    $body = json_decode(file_get_contents('php://input'), true);
    $name = trim($body['name'] ?? '');
    $text = trim($body['text'] ?? '');

    if ($name === '' || $text === '') {
        http_response_code(400);
        echo json_encode(['error' => 'Name and text are required']);
        $conn->close();
        exit;
    }

    $stmt = $conn->prepare('INSERT INTO Reviews (Name, Review) VALUES (?, ?)');
    $stmt->bind_param('ss', $name, $text);
    $stmt->execute();
    $id = $conn->insert_id;
    $stmt->close();

    $row  = $conn->query("SELECT Name, Review, DateTime FROM Reviews WHERE Id = $id")->fetch_assoc();
    http_response_code(201);
    echo json_encode([
        'name'      => $row['Name'],
        'text'      => $row['Review'],
        'createdAt' => date('Y-m-d\TH:i:s', strtotime($row['DateTime'])),
    ]);

} else {
    http_response_code(405);
    echo json_encode(['error' => 'Method not allowed']);
}

$conn->close();
