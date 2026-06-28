import {
  Badge,
  Button,
  Card,
  Center,
  Group,
  Modal,
  ScrollArea,
  Select,
  Stack,
  Text,
  Textarea,
  Title,
  NumberInput,
} from '@mantine/core'
import { showNotification } from '@mantine/notifications'
import { mdiCheck, mdiClose, mdiBugCheckOutline } from '@mdi/js'
import { Icon } from '@mdi/react'
import dayjs from 'dayjs'
import { FC, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useParams } from 'react-router'
import { WithGameEditTab } from '@Components/admin/WithGameEditTab'
import { showErrorMsg } from '@Utils/Shared'
import api, { ZeroDayTicketDto, ZeroDayTicketStatus } from '@Api'

const statusColor: Record<ZeroDayTicketStatus, string> = {
  [ZeroDayTicketStatus.Pending]: 'yellow',
  [ZeroDayTicketStatus.Approved]: 'teal',
  [ZeroDayTicketStatus.Rejected]: 'red',
}

const statusLabel: Record<ZeroDayTicketStatus, string> = {
  [ZeroDayTicketStatus.Pending]: 'Pending',
  [ZeroDayTicketStatus.Approved]: 'Approved',
  [ZeroDayTicketStatus.Rejected]: 'Rejected',
}

const ReviewModal: FC<{
  opened: boolean
  onClose: () => void
  ticket: ZeroDayTicketDto | null
  action: 'approve' | 'reject'
  gameId: number
  onReviewed: () => void
}> = ({ opened, onClose, ticket, action, gameId, onReviewed }) => {
  const { t } = useTranslation()
  const [disabled, setDisabled] = useState(false)
  const [bonusScore, setBonusScore] = useState(0)
  const [comment, setComment] = useState('')
  const [challengeId, setChallengeId] = useState<number | null>(null)

  const { data: challenges } = api.edit.useEditGetGameChallenges(gameId)

  const onSubmit = async () => {
    if (!ticket?.id) return
    if (action === 'reject' && !comment.trim()) {
      showNotification({
        color: 'red',
        message: t('admin.notification.zeroday.comment_required', {
          defaultValue: 'Comment is required for rejection',
        }),
      })
      return
    }
    setDisabled(true)
    try {
      if (action === 'approve') {
        await api.zeroDayTicket.zeroDayTicketApprove(ticket.id, {
          bonusScore,
          comment,
          challengeId: ticket.challengeId ? undefined : (challengeId ?? undefined),
        })
      } else {
        await api.zeroDayTicket.zeroDayTicketReject(ticket.id, { bonusScore, comment })
      }
      showNotification({
        color: 'teal',
        message: t('admin.notification.zeroday.reviewed', { defaultValue: 'Ticket reviewed' }),
        icon: <Icon path={mdiCheck} size={1} />,
      })
      onReviewed()
      onClose()
    } catch (e) {
      showErrorMsg(e, t)
    } finally {
      setDisabled(false)
    }
  }

  const challengeOptions = (challenges ?? []).map((c) => ({ value: String(c.id ?? ''), label: c.title }))

  return (
    <Modal
      opened={opened}
      onClose={onClose}
      title={`${action === 'approve' ? 'Approve' : 'Reject'} 0day Ticket`}
      size="lg"
    >
      <Stack gap="sm">
        {ticket && (
          <Stack gap={4}>
            <Text fw={700}>{ticket.title || '(no title)'}</Text>
            <Text size="sm" c="dimmed">
              by {ticket.userName ?? '-'} | Challenge: {ticket.challengeTitle ?? '-'}
            </Text>
            <Text size="sm">{ticket.description}</Text>
          </Stack>
        )}
        {action === 'approve' && ticket && !ticket.challengeId && (
          <Stack gap={4}>
            <Text size="sm" c="orange">
              This ticket has no ChallengeId. Select a challenge to award bonus to the team.
              If no challenge is selected, bonus will not be added to the team score.
            </Text>
            <Select
              label="Challenge (optional, required for bonus)"
              value={challengeId ? String(challengeId) : null}
              onChange={(v) => setChallengeId(v ? parseInt(v) : null)}
              data={challengeOptions}
              searchable
              placeholder="Select a challenge"
            />
          </Stack>
        )}
        <NumberInput
          label="Bonus Score"
          value={bonusScore}
          onChange={(v) => setBonusScore(typeof v === 'number' ? v : 0)}
          min={0}
        />
        <Textarea
          label="Comment"
          placeholder={action === 'reject' ? 'Required for rejection' : 'Optional'}
          value={comment}
          autosize
          minRows={3}
          onChange={(e) => setComment(e.target.value)}
        />
        <Group justify="flex-end">
          <Button variant="default" onClick={onClose}>
            {t('common.button.cancel', { defaultValue: 'Cancel' })}
          </Button>
          <Button color={action === 'approve' ? 'teal' : 'red'} disabled={disabled} onClick={onSubmit}>
            {action === 'approve' ? 'Approve' : 'Reject'}
          </Button>
        </Group>
      </Stack>
    </Modal>
  )
}

const AdminZeroDayTickets: FC = () => {
  const { id } = useParams()
  const numId = parseInt(id ?? '-1')
  const { t } = useTranslation()

  const [statusFilter, setStatusFilter] = useState<ZeroDayTicketStatus | 'All'>('All')
  const { data: tickets, mutate } = api.zeroDayTicket.useZeroDayTicketGetAllTickets({
    gameId: numId,
    status: statusFilter === 'All' ? undefined : statusFilter,
  })

  const [reviewOpened, setReviewOpened] = useState(false)
  const [reviewTicket, setReviewTicket] = useState<ZeroDayTicketDto | null>(null)
  const [reviewAction, setReviewAction] = useState<'approve' | 'reject'>('approve')

  const openReview = (ticket: ZeroDayTicketDto, action: 'approve' | 'reject') => {
    setReviewTicket(ticket)
    setReviewAction(action)
    setReviewOpened(true)
  }

  return (
    <WithGameEditTab isLoading={!tickets}>
      <Stack p="md" gap="md">
        <Group justify="space-between">
          <Group>
            <Icon path={mdiBugCheckOutline} size={2} />
            <Title>0day Tickets Review</Title>
          </Group>
          <Select
            label="Status"
            value={statusFilter}
            onChange={(v) => setStatusFilter((v as ZeroDayTicketStatus | 'All') ?? 'All')}
            data={[
              { value: 'All', label: 'All' },
              { value: ZeroDayTicketStatus.Pending, label: 'Pending' },
              { value: ZeroDayTicketStatus.Approved, label: 'Approved' },
              { value: ZeroDayTicketStatus.Rejected, label: 'Rejected' },
            ]}
            w={200}
          />
        </Group>
        {tickets && tickets.length === 0 ? (
          <Center h="40vh">
            <Title order={3} c="dimmed">
              {t('admin.content.zeroday.empty', { defaultValue: 'No 0day tickets' })}
            </Title>
          </Center>
        ) : (
          <ScrollArea h="calc(100vh - 12rem)">
            <Stack gap="sm">
              {tickets?.map((ticket) => (
                <Card key={ticket.id} shadow="xs" padding="md" withBorder>
                  <Group justify="space-between" align="flex-start">
                    <Stack gap={4}>
                      <Group gap="xs">
                        <Text fw={700}>{ticket.title || '(no title)'}</Text>
                        <Badge variant="light" color={statusColor[ticket.status ?? ZeroDayTicketStatus.Pending]}>
                          {statusLabel[ticket.status ?? ZeroDayTicketStatus.Pending]}
                        </Badge>
                      </Group>
                      <Text size="sm" c="dimmed">
                        by {ticket.userName ?? '-'} | Challenge: {ticket.challengeTitle ?? '-'} |{' '}
                        {ticket.createdAt ? dayjs(ticket.createdAt).format('YYYY-MM-DD HH:mm') : ''}
                      </Text>
                      <Text size="sm" lineClamp={4}>
                        {ticket.description}
                      </Text>
                      {ticket.reviewComment && (
                        <Text size="sm" c="dimmed">
                          <strong>Review by {ticket.reviewerName ?? '-'}:</strong> {ticket.reviewComment}
                        </Text>
                      )}
                      {ticket.bonusScore && ticket.bonusScore > 0 && (
                        <Badge variant="outline" color="teal">
                          Bonus: +{ticket.bonusScore}
                        </Badge>
                      )}
                    </Stack>
                    {ticket.status === ZeroDayTicketStatus.Pending && (
                      <Group>
                        <Button
                          color="teal"
                          variant="light"
                          leftSection={<Icon path={mdiCheck} size={1} />}
                          onClick={() => openReview(ticket, 'approve')}
                        >
                          Approve
                        </Button>
                        <Button
                          color="red"
                          variant="light"
                          leftSection={<Icon path={mdiClose} size={1} />}
                          onClick={() => openReview(ticket, 'reject')}
                        >
                          Reject
                        </Button>
                      </Group>
                    )}
                  </Group>
                </Card>
              ))}
            </Stack>
          </ScrollArea>
        )}
      </Stack>
      <ReviewModal
        opened={reviewOpened}
        onClose={() => setReviewOpened(false)}
        ticket={reviewTicket}
        action={reviewAction}
        gameId={numId}
        onReviewed={() => mutate()}
      />
    </WithGameEditTab>
  )
}

export default AdminZeroDayTickets
