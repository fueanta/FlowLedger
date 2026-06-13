export function downloadCsvBlob(blob: Blob, fallbackName: string, contentDisposition?: string) {
  downloadFileBlob(blob, fallbackName, contentDisposition)
}

export function downloadFileBlob(blob: Blob, fallbackName: string, contentDisposition?: string) {
  const fileName = contentDisposition ? parseFileName(contentDisposition) ?? fallbackName : fallbackName
  const url = URL.createObjectURL(blob)
  const link = document.createElement('a')
  link.href = url
  link.download = fileName
  document.body.appendChild(link)
  link.click()
  link.remove()
  URL.revokeObjectURL(url)
}

function parseFileName(contentDisposition: string) {
  const match = /filename="?([^";]+)"?/i.exec(contentDisposition)
  return match?.[1]
}
