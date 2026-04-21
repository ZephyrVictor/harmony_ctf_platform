import {
  Badge,
  Button,
  Center,
  Code,
  Group,
  Modal,
  NumberInput,
  Pagination,
  ScrollArea,
  Select,
  Stack,
  Table,
  Text,
  Textarea,
  Title,
} from '@mantine/core'
import { useDisclosure } from '@mantine/hooks'
import dayjs from 'dayjs'
import { FC, useCallback, useEffect, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { WithGameEditTab } from '@Components/admin/WithGameEditTab'
import { showErrorMsg, showSuccessMsg } from '@Utils/Shared'
import { approveTicket, getAllTickets, getSettings, rejectTicket, TicketDto } from '@Utils/ExtApi'

const TicketStatusOptions = [
  { value: '', label: 'All' },
  { value: 'Pending', label: 'Pending' },
  { value: 'Approved', label: 'Approved' },
  { value: 'Rejected', label: 'Rejected' },
]

const AdminTickets: FC = () => {
  const { t } = useTranslation()
  const [tickets, setTickets] = useState<TicketDto[]>([])
  const [loading, setLoading] = useState(true)
  const [statusFilter, setStatusFilter] = useState<string>('')
  const [page, setPage] = useState(1)
  const perPage = 20

  const [reviewModal, reviewModalHandlers] = useDisclosure(false)
  const [selectedTicket, setSelectedTicket] = useState<TicketDto | null>(null)
  const [refundScore, setRefundScore] = useState(0)
  const [bonusScore, setBonusScore] = useState(0)
  const [comment, setComment] = useState('')
  const [saving, setSaving] = useState(false)

  const load = useCallback(async () => {
    try {
      const data = await getAllTickets(statusFilter || undefined, 200, 0)
      setTickets(data)
    } catch (err: any) {
      showErrorMsg(err, t)
    } finally {
      setLoading(false)
    }
  }, [statusFilter, t])

  useEffect(() => {
    load()
  }, [load])

  const openApprove = async (ticket: TicketDto) => {
    setSelectedTicket(ticket)
    setBonusScore(0)
    setComment('')
    reviewModalHandlers.open()

    try {
      const settings = await getSettings()
      const enableAutoRefund = settings['Ticket.EnableAutoRefund'] === 'true'
      if (enableAutoRefund) {
        const ratio = Number(settings['Ticket.DefaultRefundRatio']) || 1.0
        setRefundScore(Math.round(ticket.penaltyScore * ratio))
      } else {
        setRefundScore(0)
      }
    } catch {
      setRefundScore(0)
    }
  }

  const handleApprove = async () => {
    if (!selectedTicket) return
    setSaving(true)
    try {
      await approveTicket(selectedTicket.id, { refundScore, bonusScore, comment: comment || undefined })
      showSuccessMsg(t('common.content.save_success'), t)
      reviewModalHandlers.close()
      await load()
    } catch (err: any) {
      showErrorMsg(err, t)
    } finally {
      setSaving(false)
    }
  }

  const handleReject = async (ticket: TicketDto) => {
    const reason = prompt(t('admin.ticket.reject_reason'))
    if (reason === null || !reason.trim()) return
    setSaving(true)
    try {
      await rejectTicket(ticket.id, { comment: reason.trim() })
      showSuccessMsg(t('common.content.save_success'), t)
      await load()
    } catch (err: any) {
      showErrorMsg(err, t)
    } finally {
      setSaving(false)
    }
  }

  const pagedTickets = tickets.slice((page - 1) * perPage, page * perPage)

  return (
    <WithGameEditTab
      isLoading={loading}
      head={
        <Group justify="space-between" w="100%">
          <Title order={3}>{t('admin.ticket.title')}</Title>
          <Select
            w="10rem"
            data={TicketStatusOptions.map((o) => ({ value: o.value, label: o.label }))}
            value={statusFilter}
            onChange={(v) => { setStatusFilter(v ?? ''); setPage(1) }}
          />
        </Group>
      }
    >
      {tickets.length === 0 && !loading ? (
        <Center h="10rem">
          <Text c="dimmed">{t('admin.ticket.empty')}</Text>
        </Center>
      ) : (
        <>
          <ScrollArea type="never" h="calc(100vh - 16rem)">
            <Table>
              <Table.Thead>
                <Table.Tr>
                  <Table.Th>{t('admin.ticket.col.user')}</Table.Th>
                  <Table.Th>{t('admin.ticket.col.challenge')}</Table.Th>
                  <Table.Th>{t('admin.ticket.col.answer')}</Table.Th>
                  <Table.Th>{t('admin.ticket.col.description')}</Table.Th>
                  <Table.Th>{t('admin.ticket.col.status')}</Table.Th>
                  <Table.Th>{t('admin.ticket.col.time')}</Table.Th>
                  <Table.Th>{t('admin.ticket.col.actions')}</Table.Th>
                </Table.Tr>
              </Table.Thead>
              <Table.Tbody>
                {pagedTickets.map((ticket) => (
                  <Table.Tr key={ticket.id}>
                    <Table.Td>
                      <Text size="sm">{ticket.userName ?? '—'}</Text>
                    </Table.Td>
                    <Table.Td>
                      <Text size="sm">{ticket.challengeName ?? '—'}</Text>
                    </Table.Td>
                    <Table.Td>
                      <Code>{ticket.answer ?? '—'}</Code>
                    </Table.Td>
                    <Table.Td>
                      <Text size="sm" lineClamp={2}>{ticket.description}</Text>
                    </Table.Td>
                    <Table.Td>
                      <Badge
                        color={
                          ticket.status === 'Approved' ? 'green'
                          : ticket.status === 'Rejected' ? 'red'
                          : 'yellow'
                        }
                        variant="light"
                      >
                        {ticket.status}
                      </Badge>
                    </Table.Td>
                    <Table.Td>
                      <Text size="sm">{dayjs(ticket.createdAt).format('L LT')}</Text>
                    </Table.Td>
                    <Table.Td>
                      {ticket.status === 'Pending' ? (
                        <Group gap="xs">
                          <Button size="xs" color="green" onClick={() => openApprove(ticket)}>
                            {t('admin.ticket.approve')}
                          </Button>
                          <Button size="xs" color="red" variant="outline" onClick={() => handleReject(ticket)}>
                            {t('admin.ticket.reject')}
                          </Button>
                        </Group>
                      ) : (
                        <Text size="xs" c="dimmed">
                          {ticket.reviewerName ?? '—'}
                          {ticket.reviewedAt && ` ${dayjs(ticket.reviewedAt).format('L LT')}`}
                        </Text>
                      )}
                    </Table.Td>
                  </Table.Tr>
                ))}
              </Table.Tbody>
            </Table>
          </ScrollArea>
          <Pagination
            value={page}
            onChange={setPage}
            total={Math.ceil(tickets.length / perPage)}
          />
        </>
      )}
      <Modal
        opened={reviewModal}
        onClose={reviewModalHandlers.close}
        title={t('admin.ticket.approve_title')}
      >
        <Stack gap="md">
          {selectedTicket && (
            <Stack gap="xs">
              <Text size="sm" fw="bold">{selectedTicket.userName} — {selectedTicket.challengeName}</Text>
              <Code>{selectedTicket.answer}</Code>
              <Text size="sm">{selectedTicket.description}</Text>
            </Stack>
          )}
          <NumberInput
            label={t('admin.ticket.refund_score')}
            value={refundScore}
            onChange={(v) => setRefundScore(Number(v))}
            min={0}
          />
          <NumberInput
            label={t('admin.ticket.bonus_score')}
            value={bonusScore}
            onChange={(v) => setBonusScore(Number(v))}
            min={0}
          />
          <Textarea
            label={t('admin.ticket.comment')}
            value={comment}
            onChange={(e) => setComment(e.currentTarget.value)}
            minRows={2}
          />
          <Group justify="flex-end">
            <Button variant="default" onClick={reviewModalHandlers.close}>
              {t('common.content.cancel')}
            </Button>
            <Button onClick={handleApprove} loading={saving} color="green">
              {t('admin.ticket.approve')}
            </Button>
          </Group>
        </Stack>
      </Modal>
    </WithGameEditTab>
  )
}

export default AdminTickets
