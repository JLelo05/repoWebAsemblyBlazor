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

$conn   = getDbConnection();
$method = $_SERVER['REQUEST_METHOD'];

if ($method === 'GET') {
    $date = $_GET['date'] ?? null;

    if ($date !== null) {
        $stmt = $conn->prepare('SELECT Id, Name, Term, Lessons, Phone FROM Users WHERE DATE(Term) = ? ORDER BY Term ASC');
        $stmt->bind_param('s', $date);
        $stmt->execute();
        $result = $stmt->get_result();
    } else {
        $result = $conn->query('SELECT Id, Name, Term, Lessons, Phone FROM Users ORDER BY Term ASC');
    }

    $users = [];
    while ($row = $result->fetch_assoc()) {
        $users[] = [
            'id'      => (int)$row['Id'],
            'name'    => $row['Name'],
            'term'    => date('Y-m-d\TH:i:s', strtotime($row['Term'])),
            'lessons' => (int)$row['Lessons'],
            'phone'   => $row['Phone'],
        ];
    }
    echo json_encode($users);

} elseif ($method === 'POST') {
    $body    = json_decode(file_get_contents('php://input'), true);
    $name    = trim($body['name']    ?? '');
    $term    = trim($body['term']    ?? '');
    $lessons = (int)($body['lessons'] ?? 1);
    $phone   = trim($body['phone']   ?? '');

    if ($name === '' || $term === '' || $phone === '') {
        http_response_code(400);
        echo json_encode(['error' => 'Name, term and phone are required']);
        $conn->close();
        exit;
    }

    $stmt = $conn->prepare('INSERT INTO Users (Name, Term, Lessons, Phone) VALUES (?, ?, ?, ?)');
    $stmt->bind_param('ssis', $name, $term, $lessons, $phone);
    $stmt->execute();
    $id = $conn->insert_id;
    $stmt->close();

    $row = $conn->query("SELECT Id, Name, Term, Lessons, Phone FROM Users WHERE Id = $id")->fetch_assoc();
    http_response_code(201);
    echo json_encode([
        'id'      => (int)$row['Id'],
        'name'    => $row['Name'],
        'term'    => date('Y-m-d\TH:i:s', strtotime($row['Term'])),
        'lessons' => (int)$row['Lessons'],
        'phone'   => $row['Phone'],
    ]);

} else {
    http_response_code(405);
    echo json_encode(['error' => 'Method not allowed']);
}

$conn->close();
