<?php
/**
 * ONE-TIME SETUP SCRIPT
 * 1. Upload this file to your server alongside config.php
 * 2. Open it in a browser: https://your-domain/api/setup.php
 * 3. Copy the printed ENCRYPTED_PASSWORD value into config.php
 * 4. DELETE this file from the server immediately after
 */

define('ENCRYPTION_KEY', 'L8kQv3nP9wXmZ2rY7tBjF4cHdNsUeAoG');

$plainPassword = 'f25g-.AvAI-62fRq%0#6';
$iv            = substr(hash('sha256', ENCRYPTION_KEY, true), 0, 16);
$encrypted     = base64_encode(openssl_encrypt(
    $plainPassword,
    'AES-256-CBC',
    ENCRYPTION_KEY,
    OPENSSL_RAW_DATA,
    $iv
));

echo '<pre>';
echo "Copy this value into config.php as DB_PASSWORD_ENCRYPTED:\n\n";
echo $encrypted . "\n";
echo '</pre>';
