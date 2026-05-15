import {
  ActionIcon,
  Badge,
  Button,
  FileButton,
  Group,
  Image,
  Paper,
  SimpleGrid,
  Stack,
  Text,
} from '@mantine/core'
import { mdiClose, mdiImageMultipleOutline, mdiImagePlus, mdiOpenInNew } from '@mdi/js'
import { Icon } from '@mdi/react'
import { FC, useMemo, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { HunamizeSize, showErrorMsg, showSuccessMsg } from '@Utils/Shared'
import { TicketAttachmentDto, uploadTicketAttachments } from '@Utils/ExtApi'

interface TicketAttachmentListProps {
  attachments?: TicketAttachmentDto[]
  ticketId?: string
  canUpload?: boolean
  onUploaded?: (attachments: TicketAttachmentDto[]) => void
}

interface TicketAttachmentSummaryProps {
  attachments?: TicketAttachmentDto[]
}

interface TicketAttachmentPickerProps {
  files: File[]
  onChange: (files: File[]) => void
  disabled?: boolean
}

const MAX_ATTACHMENTS = 5
const MAX_ATTACHMENT_SIZE = 10 * 1024 * 1024

const getAttachmentName = (attachment: TicketAttachmentDto) =>
  attachment.fileName ?? attachment.filename ?? attachment.url.split('/').pop() ?? 'image'

const getAttachmentSize = (attachment: TicketAttachmentDto) =>
  attachment.fileSize ?? attachment.sizeBytes ?? attachment.size

const normalizeFiles = (files: File[] | File | null) => {
  if (!files) return []
  return Array.isArray(files) ? files : [files]
}

const validateFiles = (
  files: File[],
  currentCount: number,
  t: (key: string, values?: Record<string, unknown>) => string
) => {
  if (files.length === 0) return false

  if (currentCount + files.length > MAX_ATTACHMENTS) {
    showErrorMsg(new Error(t('game.content.ticket.attachment_too_many')), t)
    return false
  }

  const invalidType = files.find((file) => !file.type.startsWith('image/'))
  if (invalidType) {
    showErrorMsg(new Error(t('game.content.ticket.attachment_invalid_type')), t)
    return false
  }

  const oversized = files.find((file) => file.size > MAX_ATTACHMENT_SIZE)
  if (oversized) {
    showErrorMsg(new Error(t('game.content.ticket.attachment_too_large')), t)
    return false
  }

  return true
}

export const TicketAttachmentSummary: FC<TicketAttachmentSummaryProps> = ({ attachments }) => {
  const { t } = useTranslation()
  const count = attachments?.length ?? 0

  if (count === 0) return null

  return (
    <Badge size="xs" variant="light" leftSection={<Icon path={mdiImageMultipleOutline} size={0.55} />}>
      {t('game.content.ticket.attachment_count', { count })}
    </Badge>
  )
}

export const TicketAttachmentPicker: FC<TicketAttachmentPickerProps> = ({ files, onChange, disabled }) => {
  const { t } = useTranslation()
  const remaining = Math.max(0, MAX_ATTACHMENTS - files.length)

  const handleSelect = (value: File[] | File | null) => {
    const selected = normalizeFiles(value)
    if (!validateFiles(selected, files.length, t)) return
    onChange([...files, ...selected])
  }

  const removeFile = (index: number) => {
    onChange(files.filter((_, i) => i !== index))
  }

  return (
    <Stack gap="xs">
      <Group justify="space-between" align="center">
        <Group gap="xs">
          <Text size="sm" c="dimmed">
            {t('game.content.ticket.attachments')}
          </Text>
          <Badge size="xs" variant="light">
            {files.length}/{MAX_ATTACHMENTS}
          </Badge>
        </Group>
        <FileButton onChange={handleSelect} accept="image/*" multiple>
          {(props) => (
            <Button
              {...props}
              size="xs"
              variant="light"
              disabled={disabled || remaining === 0}
              leftSection={<Icon path={mdiImagePlus} size={0.7} />}
            >
              {t('game.content.ticket.upload_images')}
            </Button>
          )}
        </FileButton>
      </Group>
      <Text size="xs" c="dimmed">
        {remaining > 0
          ? t('game.content.ticket.upload_help')
          : t('game.content.ticket.attachment_limit_reached')}
      </Text>
      {files.length > 0 && (
        <Stack gap={4}>
          {files.map((file, index) => (
            <Paper key={`${file.name}-${file.lastModified}-${index}`} withBorder p="xs" radius="sm">
              <Group wrap="nowrap" gap="xs">
                <Icon path={mdiImageMultipleOutline} size={0.85} />
                <Stack gap={0} style={{ minWidth: 0, flex: 1 }}>
                  <Text size="sm" truncate>{file.name}</Text>
                  <Text size="xs" c="dimmed">{HunamizeSize(file.size)}</Text>
                </Stack>
                <ActionIcon
                  variant="subtle"
                  color="gray"
                  onClick={() => removeFile(index)}
                  aria-label={t('common.content.delete')}
                  disabled={disabled}
                >
                  <Icon path={mdiClose} size={0.75} />
                </ActionIcon>
              </Group>
            </Paper>
          ))}
        </Stack>
      )}
    </Stack>
  )
}

export const TicketAttachmentList: FC<TicketAttachmentListProps> = ({
  attachments,
  ticketId,
  canUpload = false,
  onUploaded,
}) => {
  const { t } = useTranslation()
  const [uploading, setUploading] = useState(false)
  const items = useMemo(() => attachments ?? [], [attachments])
  const remaining = Math.max(0, MAX_ATTACHMENTS - items.length)
  const uploadDisabled = uploading || !canUpload || !ticketId || remaining === 0

  const handleUpload = async (value: File[] | File | null) => {
    const files = normalizeFiles(value)
    if (!ticketId || !validateFiles(files, items.length, t)) return

    setUploading(true)
    try {
      const attachments = await uploadTicketAttachments(ticketId, files)
      onUploaded?.(attachments)
      showSuccessMsg(t('common.content.save_success'), t)
    } catch (err: any) {
      showErrorMsg(err, t)
    } finally {
      setUploading(false)
    }
  }

  return (
    <Stack gap="xs">
      <Group justify="space-between" align="center">
        <Group gap="xs">
          <Text size="sm" c="dimmed">
            {t('game.content.ticket.attachments')}
          </Text>
          <Badge size="xs" variant="light">
            {items.length}/{MAX_ATTACHMENTS}
          </Badge>
        </Group>
        {canUpload && (
          <FileButton onChange={handleUpload} accept="image/*" multiple>
            {(props) => (
              <Button
                {...props}
                size="xs"
                variant="light"
                loading={uploading}
                disabled={uploadDisabled}
                leftSection={<Icon path={mdiImagePlus} size={0.7} />}
              >
                {t('game.content.ticket.upload_images')}
              </Button>
            )}
          </FileButton>
        )}
      </Group>

      {canUpload && (
        <Text size="xs" c="dimmed">
          {remaining > 0
            ? t('game.content.ticket.upload_help')
            : t('game.content.ticket.attachment_limit_reached')}
        </Text>
      )}

      {items.length === 0 ? (
        <Paper withBorder p="sm" radius="sm">
          <Text size="sm" c="dimmed">
            {t('game.content.ticket.no_attachments')}
          </Text>
        </Paper>
      ) : (
        <SimpleGrid cols={{ base: 1, sm: 2 }} spacing="xs">
          {items.map((attachment) => {
            const name = getAttachmentName(attachment)
            const size = getAttachmentSize(attachment)

            return (
              <Paper key={attachment.id} withBorder p="xs" radius="sm">
                <Group wrap="nowrap" align="center">
                  <Image src={attachment.url} alt={name} w={72} h={54} fit="cover" radius="sm" />
                  <Stack gap={2} style={{ minWidth: 0, flex: 1 }}>
                    <Text size="sm" fw={500} truncate>
                      {name}
                    </Text>
                    {size !== undefined && (
                      <Text size="xs" c="dimmed">
                        {HunamizeSize(size)}
                      </Text>
                    )}
                  </Stack>
                  <ActionIcon
                    component="a"
                    href={attachment.url}
                    target="_blank"
                    rel="noopener noreferrer"
                    variant="subtle"
                    aria-label={t('game.content.ticket.open_attachment')}
                  >
                    <Icon path={mdiOpenInNew} size={0.8} />
                  </ActionIcon>
                </Group>
              </Paper>
            )
          })}
        </SimpleGrid>
      )}
    </Stack>
  )
}
