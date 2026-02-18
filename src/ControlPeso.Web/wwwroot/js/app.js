// Helper function to download file from base64
function downloadFileFromBase64(filename, contentType, base64Content) {
    const linkSource = `data:${contentType};base64,${base64Content}`;
    const downloadLink = document.createElement('a');
    document.body.appendChild(downloadLink);

    downloadLink.href = linkSource;
    downloadLink.download = filename;
    downloadLink.click();

    document.body.removeChild(downloadLink);
}
