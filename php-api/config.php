<?php
// ── Database connection config ────────────────────────────────
// Password is AES-256-CBC encrypted; plain text is never stored here.
// Run setup.php once on the server to regenerate DB_PASSWORD_ENCRYPTED,
// then delete setup.php from the server.

define('DB_HOST',               'sql6.webzdarma.cz');
define('DB_NAME',               'lelopageeu1876');
define('DB_USER',               'lelopageeu1876');
define('ENCRYPTION_KEY',        'L8kQv3nP9wXmZ2rY7tBjF4cHdNsUeAoG');
define('DB_PASSWORD_ENCRYPTED', '/tZeEx2gM0k07YVaWQ/VQli4LomSyvlkMnlipSdtrtU=');

function getDbPassword(): string
{
    $iv = substr(hash('sha256', ENCRYPTION_KEY, true), 0, 16);
    return openssl_decrypt(
        base64_decode(DB_PASSWORD_ENCRYPTED),
        'AES-256-CBC',
        ENCRYPTION_KEY,
        OPENSSL_RAW_DATA,
        $iv
    );
}

function getDbConnection(): mysqli
{
    $conn = new mysqli(DB_HOST, DB_USER, getDbPassword(), DB_NAME);
    if ($conn->connect_error) {
        http_response_code(500);
        echo json_encode(['error' => 'Database connection failed']);
        exit;
    }
    $conn->set_charset('utf8mb4');
    return $conn;
}
