import { Paper, SegmentedControl, Stack, Text, Textarea, TextareaProps, Typography } from '@mantine/core'
import { Marked } from 'marked'
import { FC, useMemo, useState } from 'react'
import { useTranslation } from 'react-i18next'
import type { RendererThis, Tokens } from 'marked'
import classes from '@Styles/Typography.module.css'

interface TicketDescriptionPreviewProps {
  source: string
}

interface TicketDescriptionEditorProps extends Omit<TextareaProps, 'onChange' | 'value'> {
  value: string
  onChange: (value: string) => void
}

const allowedProtocols = ['http:', 'https:', 'mailto:', 'tel:']

const escapeHtml = (value: string) => value
  .replace(/&/g, '&amp;')
  .replace(/</g, '&lt;')
  .replace(/>/g, '&gt;')
  .replace(/"/g, '&quot;')
  .replace(/'/g, '&#39;')

const isSafeUrl = (url: string) => {
  const normalized = url.trim().replace(/[\u0000-\u001F\u007F\s]+/g, '')

  if (
    normalized.startsWith('#') ||
    normalized.startsWith('/') ||
    normalized.startsWith('./') ||
    normalized.startsWith('../') ||
    normalized.startsWith('?')
  ) {
    return true
  }

  if (normalized.startsWith('//')) {
    return true
  }

  const scheme = normalized.match(/^[a-z][a-z0-9+.-]*:/i)?.[0].toLowerCase()
  return scheme ? allowedProtocols.includes(scheme) : true
}

const createSafeMarkdown = () => {
  const marked = new Marked()

  marked.use({
    renderer: {
      html({ text }: Tokens.HTML | Tokens.Tag) {
        return escapeHtml(text)
      },
      link(this: RendererThis, { href, title, tokens }: Tokens.Link) {
        const label = this.parser.parseInline(tokens)

        if (!isSafeUrl(href)) {
          return label
        }

        const safeTitle = title ? ` title="${escapeHtml(title)}"` : ''
        return `<a href="${escapeHtml(href.trim())}"${safeTitle} rel="nofollow noopener noreferrer">${label}</a>`
      },
      image({ href, title, text }: Tokens.Image) {
        if (!isSafeUrl(href)) {
          return escapeHtml(text)
        }

        const safeTitle = title ? ` title="${escapeHtml(title)}"` : ''
        return `<img src="${escapeHtml(href.trim())}" alt="${escapeHtml(text)}"${safeTitle}>`
      },
    },
  }).setOptions({ silent: true })

  return marked
}

const SafeMarkdown: FC<{ source: string }> = ({ source }) => {
  const marked = useMemo(createSafeMarkdown, [])
  const html = useMemo(() => marked.parse(source), [marked, source])

  return (
    <Typography>
      <div className={classes.root} dangerouslySetInnerHTML={{ __html: html ?? '' }} />
    </Typography>
  )
}

export const TicketDescriptionPreview: FC<TicketDescriptionPreviewProps> = ({ source }) => {
  const { t } = useTranslation()
  const content = source.trim()

  return (
    <Paper withBorder p="sm" radius="sm">
      {content ? (
        <SafeMarkdown source={content} />
      ) : (
        <Text size="sm" c="dimmed">
          {t('game.content.ticket.no_description')}
        </Text>
      )}
    </Paper>
  )
}

export const TicketDescriptionEditor: FC<TicketDescriptionEditorProps> = (props) => {
  const { value, onChange, description, minRows = 5, ...others } = props
  const { t } = useTranslation()
  const [mode, setMode] = useState('edit')

  return (
    <Stack gap="xs">
      <SegmentedControl
        value={mode}
        onChange={setMode}
        data={[
          { value: 'edit', label: t('game.content.ticket.edit') },
          { value: 'preview', label: t('game.content.ticket.preview') },
        ]}
      />
      {mode === 'preview' ? (
        <TicketDescriptionPreview source={value} />
      ) : (
        <Textarea
          {...others}
          value={value}
          onChange={(event) => onChange(event.currentTarget.value)}
          description={description ?? t('admin.content.markdown_support')}
          minRows={minRows}
        />
      )}
    </Stack>
  )
}
