import { Button, Checkbox, Group, Modal, ModalProps, Stack } from '@mantine/core'
import { FC, useEffect, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { PDFViewer } from '@Components/admin/PDFViewer'

interface GameCommitmentModalProps extends ModalProps {
  onConfirm: () => void
}

export const GameCommitmentModal: FC<GameCommitmentModalProps> = (props) => {
  const { onConfirm, ...modalProps } = props
  const [agreed, setAgreed] = useState(false)
  const { t } = useTranslation()

  useEffect(() => {
    if (props.opened) setAgreed(false)
  }, [props.opened])

  const pdfUrl = `${import.meta.env.BASE_URL}pdf/commitment.pdf`

  return (
    <Modal {...modalProps} size="70vw" title={t('game.content.commitment.title')}>
      <Stack gap="md">
        <PDFViewer url={pdfUrl} height="65vh" />
        <Checkbox
          checked={agreed}
          onChange={(e) => setAgreed(e.currentTarget.checked)}
          label={t('game.content.commitment.agree')}
          size="md"
        />
        <Group justify="flex-end" gap="sm">
          <Button variant="default" onClick={props.onClose}>
            {t('game.content.commitment.cancel')}
          </Button>
          <Button disabled={!agreed} onClick={onConfirm}>
            {t('game.content.commitment.next')}
          </Button>
        </Group>
      </Stack>
    </Modal>
  )
}
